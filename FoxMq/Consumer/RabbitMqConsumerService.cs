using FoxMq.Configuration;
using FoxMq.Connection;
using FoxMq.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoxMq.Consumer;

/// <summary>
/// Background service that consumes messages from a RabbitMQ queue and dispatches them to a consumer.
/// </summary>
/// <typeparam name="TConsumer">The consumer type to handle messages.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class RabbitMqConsumerService<TConsumer, TMessage> : BackgroundService
    where TConsumer : class, IRabbitMqConsumer<TMessage>
{
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly ILogger<RabbitMqConsumerService<TConsumer, TMessage>> _logger;
    private readonly QueueConfig _queueConfig;
    private readonly IServiceScopeFactory _scopeFactory;

    private IChannel? _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConsumerService{TConsumer, TMessage}"/> class.
    /// </summary>
    public RabbitMqConsumerService(
        RabbitMqConnectionHolder connectionHolder,
        IServiceScopeFactory scopeFactory,
        QueueConfig queueConfig,
        ILogger<RabbitMqConsumerService<TConsumer, TMessage>> logger)
    {
        _connectionHolder = connectionHolder ?? throw new ArgumentNullException(nameof(connectionHolder));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _queueConfig = queueConfig ?? throw new ArgumentNullException(nameof(queueConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting consumer {ConsumerType} for queue '{QueueName}' with message type {MessageType}",
            typeof(TConsumer).Name, _queueConfig.QueueName, typeof(TMessage).Name);

        _channel = await _connectionHolder.CreateChannelAsync(stoppingToken).ConfigureAwait(false);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _queueConfig.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        await _channel.QueueDeclareAsync(
            queue: _queueConfig.QueueName,
            durable: _queueConfig.Durable,
            exclusive: _queueConfig.Exclusive,
            autoDelete: _queueConfig.AutoDelete,
            arguments: _queueConfig.BuildArguments(),
            cancellationToken: stoppingToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Queue '{QueueName}' declared (durable={Durable}, exclusive={Exclusive}, autoDelete={AutoDelete}, prefetch={Prefetch})",
            _queueConfig.QueueName, _queueConfig.Durable, _queueConfig.Exclusive,
            _queueConfig.AutoDelete, _queueConfig.PrefetchCount);

        await DeclareExchangeAndBindQueueAsync(stoppingToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea, stoppingToken).ConfigureAwait(false);

        await _channel.BasicConsumeAsync(
            queue: _queueConfig.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        _logger.LogInformation("Consumer {ConsumerType} is now listening on queue '{QueueName}'",
            typeof(TConsumer).Name, _queueConfig.QueueName);

        // Keep the service running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var deliveryTag = ea.DeliveryTag;

        try
        {
            _logger.LogDebug(
                "Received message on queue '{QueueName}' (deliveryTag={DeliveryTag})",
                _queueConfig.QueueName, deliveryTag);

            await using var scope = _scopeFactory.CreateAsyncScope();

            // Get deserializer from DI (will be JsonMessageDeserializer by default)
            var deserializer = scope.ServiceProvider.GetRequiredService<IMessageDeserializer<TMessage>>();
            var message = deserializer.Deserialize(ea.Body.Span);

            var consumerInstance = scope.ServiceProvider.GetRequiredService<TConsumer>();
            await consumerInstance.HandleMessageAsync(message, stoppingToken).ConfigureAwait(false);

            if (_channel is not null)
            {
                await _channel.BasicAckAsync(deliveryTag, multiple: false, stoppingToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Message acknowledged (deliveryTag={DeliveryTag})", deliveryTag);
        }
        catch (MessageDeserializationException ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize message on queue '{QueueName}' (deliveryTag={DeliveryTag}). Message will be requeued.",
                _queueConfig.QueueName, deliveryTag);

            await NackAndDeadLetter(deliveryTag, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Error processing message on queue '{QueueName}' (deliveryTag={DeliveryTag}). Message will be requeued.",
                _queueConfig.QueueName, deliveryTag);

            await NackMessageAsync(deliveryTag, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task NackMessageAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.BasicRejectAsync(deliveryTag, requeue, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to nack message (deliveryTag={DeliveryTag})", deliveryTag);
        }
    }

    private Task NackAndDeadLetter(ulong deliveryTag, CancellationToken cancellationToken)
        => NackMessageAsync(deliveryTag, requeue: false, cancellationToken);

    private Task NackMessageAsync(ulong deliveryTag, CancellationToken cancellationToken) =>
        NackMessageAsync(deliveryTag, requeue: true, cancellationToken);

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping consumer {ConsumerType} for queue '{QueueName}'",
            typeof(TConsumer).Name, _queueConfig.QueueName);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken).ConfigureAwait(false);
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }
    }

    private async Task DeclareExchangeAndBindQueueAsync(CancellationToken cancellationToken)
    {
        var exchange = _queueConfig.Exchange;
        if (exchange is null) return;

        await _channel!.ExchangeDeclareAsync(
            exchange: exchange.ExchangeName,
            type: exchange.ExchangeType,
            durable: exchange.Durable,
            autoDelete: exchange.AutoDelete,
            arguments: exchange.Arguments,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Exchange '{ExchangeName}' declared (type={ExchangeType}, durable={Durable})",
            exchange.ExchangeName, exchange.ExchangeType, exchange.Durable);

        await _channel!.QueueBindAsync(
            queue: _queueConfig.QueueName,
            exchange: exchange.ExchangeName,
            routingKey: _queueConfig.RoutingKey,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Queue '{QueueName}' bound to exchange '{ExchangeName}' with routing key '{RoutingKey}'",
            _queueConfig.QueueName, exchange.ExchangeName, _queueConfig.RoutingKey);
    }
}