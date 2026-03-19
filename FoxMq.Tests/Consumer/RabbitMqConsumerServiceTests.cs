using FoxMq.Configuration;
using FoxMq.Connection;
using FoxMq.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;

namespace FoxMq.Tests.Consumer;

public class RabbitMqConsumerServiceTests
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly QueueConfig _queueConfig;
    private readonly IServiceScope _scope;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _scopeServiceProvider;

    public RabbitMqConsumerServiceTests()
    {
        _connectionHolder = new RabbitMqConnectionHolder();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _scopeServiceProvider = Substitute.For<IServiceProvider>();
        _connection = Substitute.For<IConnection>();
        _channel = Substitute.For<IChannel>();
        _queueConfig = new QueueConfig
        {
            QueueName = "test-queue",
            Durable = true,
            PrefetchCount = 5
        };

        _connection.IsOpen.Returns(true);
        _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_channel);
        _scopeFactory.CreateAsyncScope().Returns(new AsyncServiceScope(_scope));
        _scope.ServiceProvider.Returns(_scopeServiceProvider);
    }

    private RabbitMqConsumerService<TestConsumer, TestMessage> CreateService() =>
        new(
            _connectionHolder,
            _scopeFactory,
            _queueConfig,
            NullLogger<RabbitMqConsumerService<TestConsumer, TestMessage>>.Instance);

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectionHolderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConsumerService<TestConsumer, TestMessage>(
            null!,
            _scopeFactory,
            _queueConfig,
            NullLogger<RabbitMqConsumerService<TestConsumer, TestMessage>>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenScopeFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConsumerService<TestConsumer, TestMessage>(
            _connectionHolder,
            null!,
            _queueConfig,
            NullLogger<RabbitMqConsumerService<TestConsumer, TestMessage>>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenQueueConfigIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConsumerService<TestConsumer, TestMessage>(
            _connectionHolder,
            _scopeFactory,
            null!,
            NullLogger<RabbitMqConsumerService<TestConsumer, TestMessage>>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConsumerService<TestConsumer, TestMessage>(
            _connectionHolder,
            _scopeFactory,
            _queueConfig,
            null!));
    }

    [Fact]
    public async Task ExecuteAsync_DeclaresQueueWithCorrectParameters()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).QueueDeclareAsync(
            queue: "test-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: Arg.Is<IDictionary<string, object?>>(d => d.Count == 0),
            cancellationToken: Arg.Any<CancellationToken>(),
            passive: true);
    }

    [Fact]
    public async Task ExecuteAsync_UsesArgumentsFromBuildArguments()
    {
        var quorumConfig = new QuorumQueueConfig
        {
            QueueName = "quorum-queue",
            Durable = true,
            PrefetchCount = 5,
            DeliveryLimit = 3
        };

        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);

        var service = new RabbitMqConsumerService<TestConsumer, TestMessage>(
            _connectionHolder,
            _scopeFactory,
            quorumConfig,
            NullLogger<RabbitMqConsumerService<TestConsumer, TestMessage>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).QueueDeclareAsync(
            queue: "quorum-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: Arg.Is<IDictionary<string, object?>>(d =>
                d.ContainsKey("x-queue-type") && (string)d["x-queue-type"]! == "quorum" &&
                d.ContainsKey("x-delivery-limit") && (int)d["x-delivery-limit"]! == 3),
            cancellationToken: Arg.Any<CancellationToken>(),
            passive: true);
    }

    [Fact]
    public async Task ExecuteAsync_SetsQosWithCorrectPrefetchCount()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 5,
            global: false,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_StartsConsumingWithManualAck()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).BasicConsumeAsync(
            queue: "test-queue",
            autoAck: false,
            consumer: Arg.Any<IAsyncBasicConsumer>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_ClosesChannel()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(50, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        await _channel.Received(1).CloseAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExchangeIsNull_DoesNotCallExchangeDeclareOrQueueBind()
    {
        _queueConfig.Exchange = null;
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.DidNotReceive().ExchangeDeclareAsync(
            exchange: Arg.Any<string>(),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            passive: Arg.Any<bool>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await _channel.DidNotReceive().QueueBindAsync(
            queue: Arg.Any<string>(),
            exchange: Arg.Any<string>(),
            routingKey: Arg.Any<string>(),
            arguments: Arg.Any<IDictionary<string, object?>?>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExchangeIsSet_CallsExchangeDeclareWithCorrectParameters()
    {
        _queueConfig.Exchange = new ExchangeConfig
        {
            ExchangeName = "orders-exchange",
            ExchangeType = "topic",
            Durable = true,
            AutoDelete = false
        };
        _queueConfig.RoutingKey = "order.#";

        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).ExchangeDeclareAsync(
            exchange: "orders-exchange",
            type: "topic",
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExchangeIsSet_CallsQueueBindWithCorrectParameters()
    {
        _queueConfig.Exchange = new ExchangeConfig { ExchangeName = "orders-exchange" };
        _queueConfig.RoutingKey = "order.created";

        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        await _channel.Received(1).QueueBindAsync(
            queue: "test-queue",
            exchange: "orders-exchange",
            routingKey: "order.created",
            arguments: null,
            noWait: false,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    public record TestMessage(string Content, int Value);

    public class TestConsumer : IRabbitMqConsumer<TestMessage>
    {
        public bool WasCalled { get; private set; }
        public TestMessage? ReceivedMessage { get; private set; }

        public Task HandleMessageAsync(TestMessage message, CancellationToken cancellationToken)
        {
            WasCalled = true;
            ReceivedMessage = message;
            return Task.CompletedTask;
        }
    }
}