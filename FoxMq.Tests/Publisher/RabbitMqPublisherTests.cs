using FoxMq.Connection;
using FoxMq.Publisher;
using FoxMq.Serialization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;

namespace FoxMq.Tests.Publisher;

public class RabbitMqPublisherTests
{
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqPublisherTests()
    {
        _connectionHolder = new RabbitMqConnectionHolder();
        _serializer = Substitute.For<IMessageSerializer>();
        _logger = Substitute.For<ILogger<RabbitMqPublisher>>();
        _connection = Substitute.For<IConnection>();
        _channel = Substitute.For<IChannel>();

        _connection.IsOpen.Returns(true);
        _channel.IsOpen.Returns(true);
        _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_channel);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectionHolderIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMqPublisher(null!, _serializer, _logger));
        Assert.Equal("connectionHolder", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSerializerIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMqPublisher(_connectionHolder, null!, _logger));
        Assert.Equal("serializer", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMqPublisher(_connectionHolder, _serializer, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsArgumentNullException_WhenMessageIsNull()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync<TestMessage>(null!, "exchange", "routingKey"));
        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsArgumentNullException_WhenExchangeIsNull()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync(new TestMessage("test"), null!, "routingKey"));
        Assert.Equal("exchange", ex.ParamName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsArgumentNullException_WhenRoutingKeyIsNull()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync(new TestMessage("test"), "exchange", null!));
        Assert.Equal("routingKey", ex.ParamName);
    }

    [Fact]
    public async Task PublishAsync_SerializesAndPublishesMessage()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test-value");
        var serializedBytes = new byte[] { 1, 2, 3 };
        _serializer.Serialize(message).Returns(serializedBytes);

        await publisher.PublishAsync(message, "test-exchange", "test.routing.key");

        _serializer.Received(1).Serialize(message);
        var calls = _channel.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name.Contains("BasicPublishAsync"))
            .ToList();
        Assert.Single(calls);
        var args = calls[0].GetArguments();
        Assert.Equal("test-exchange", args[0]);
        Assert.Equal("test.routing.key", args[1]);
    }

    [Fact]
    public async Task PublishToQueueAsync_ThrowsArgumentNullException_WhenQueueNameIsNull()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishToQueueAsync(new TestMessage("test"), null!));
        Assert.Equal("queueName", ex.ParamName);
    }

    [Fact]
    public async Task PublishToQueueAsync_PublishesToDefaultExchangeWithQueueNameAsRoutingKey()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test-value");
        var serializedBytes = new byte[] { 1, 2, 3 };
        _serializer.Serialize(message).Returns(serializedBytes);

        await publisher.PublishToQueueAsync(message, "my-queue");

        var calls = _channel.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name.Contains("BasicPublishAsync"))
            .ToList();
        Assert.Single(calls);
        var args = calls[0].GetArguments();
        Assert.Equal("", args[0]);
        Assert.Equal("my-queue", args[1]);
    }

    [Fact]
    public async Task PublishAsync_ReusesChannel_ForMultiplePublishes()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test");
        _serializer.Serialize(message).Returns(new byte[] { 1 });

        await publisher.PublishAsync(message, "exchange", "key1");
        await publisher.PublishAsync(message, "exchange", "key2");
        await publisher.PublishAsync(message, "exchange", "key3");

        await _connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_CreatesNewChannel_WhenPreviousChannelIsClosed()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test");
        _serializer.Serialize(message).Returns(new byte[] { 1 });

        await publisher.PublishAsync(message, "exchange", "key1");

        _channel.IsOpen.Returns(false);

        var newChannel = Substitute.For<IChannel>();
        newChannel.IsOpen.Returns(true);
        _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(newChannel);

        await publisher.PublishAsync(message, "exchange", "key2");

        await _connection.Received(2).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ThrowsObjectDisposedException_AfterDispose()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        await publisher.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => publisher.PublishAsync(new TestMessage("test"), "exchange", "key"));
    }

    [Fact]
    public async Task DisposeAsync_DisposesChannel()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test");
        _serializer.Serialize(message).Returns(new byte[] { 1 });

        await publisher.PublishAsync(message, "exchange", "key");
        await publisher.DisposeAsync();

        await _channel.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        await _connectionHolder.SetConnectionAsync(_connection, CancellationToken.None);
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);
        var message = new TestMessage("test");
        _serializer.Serialize(message).Returns(new byte[] { 1 });

        await publisher.PublishAsync(message, "exchange", "key");
        await publisher.DisposeAsync();
        await publisher.DisposeAsync();

        await _channel.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow_WhenNoChannelCreated()
    {
        var publisher = new RabbitMqPublisher(_connectionHolder, _serializer, _logger);

        var exception = await Record.ExceptionAsync(async () => await publisher.DisposeAsync());

        Assert.Null(exception);
    }

    public record TestMessage(string Value);
}
