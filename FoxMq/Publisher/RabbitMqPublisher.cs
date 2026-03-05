using FoxMq.Connection;
using FoxMq.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FoxMq.Publisher;

/// <summary>
/// RabbitMQ implementation of <see cref="IMessagePublisher"/>.
/// </summary>
public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    private IChannel? _channel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqPublisher"/> class.
    /// </summary>
    /// <param name="connectionHolder">The connection holder.</param>
    /// <param name="serializer">The message serializer.</param>
    /// <param name="logger">The logger.</param>
    public RabbitMqPublisher(
        RabbitMqConnectionHolder connectionHolder,
        IMessageSerializer serializer,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionHolder = connectionHolder ?? throw new ArgumentNullException(nameof(connectionHolder));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        TMessage message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(routingKey);

        ObjectDisposedException.ThrowIf(_disposed, this);

        var body = _serializer.Serialize(message);
        var channel = await GetOrCreateChannelAsync(cancellationToken).ConfigureAwait(false);

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Published message of type {MessageType} to exchange '{Exchange}' with routing key '{RoutingKey}'",
            typeof(TMessage).Name,
            exchange,
            routingKey);
    }

    /// <inheritdoc />
    public Task PublishToQueueAsync<TMessage>(
        TMessage message,
        string queueName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueName);

        // Publish to default exchange with queue name as routing key
        return PublishAsync(message, exchange: "", routingKey: queueName, cancellationToken);
    }

    private async Task<IChannel> GetOrCreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _channelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            if (_channel is not null)
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
            }

            _channel = await _connectionHolder.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Created new channel for publisher");

            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _channelLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_channel is not null)
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
                _channel = null;
            }
        }
        finally
        {
            _channelLock.Release();
            _channelLock.Dispose();
        }
    }
}
