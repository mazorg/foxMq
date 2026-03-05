using FoxMq.Connection;
using NSubstitute;
using RabbitMQ.Client;

namespace FoxMq.Tests.Connection;

public class RabbitMqConnectionHolderTests
{
    [Fact]
    public void IsConnected_ReturnsFalse_WhenNoConnection()
    {
        var holder = new RabbitMqConnectionHolder();

        Assert.False(holder.IsConnected);
    }

    [Fact]
    public async Task IsConnected_ReturnsTrue_AfterConnectionSet()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        await holder.SetConnectionAsync(connection, CancellationToken.None);

        Assert.True(holder.IsConnected);
    }

    [Fact]
    public async Task IsConnected_ReturnsFalse_WhenConnectionClosed()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(false);

        await holder.SetConnectionAsync(connection, CancellationToken.None);

        Assert.False(holder.IsConnected);
    }

    [Fact]
    public void Connection_ThrowsInvalidOperationException_WhenNotConnected()
    {
        var holder = new RabbitMqConnectionHolder();

        var ex = Assert.Throws<InvalidOperationException>(() => holder.Connection);
        Assert.Contains("not been established", ex.Message);
    }

    [Fact]
    public async Task Connection_ReturnsConnection_WhenSet()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        await holder.SetConnectionAsync(connection, CancellationToken.None);

        Assert.Same(connection, holder.Connection);
    }

    [Fact]
    public async Task SetConnectionAsync_DisposesOldConnection()
    {
        var holder = new RabbitMqConnectionHolder();
        var oldConnection = Substitute.For<IConnection>();
        var newConnection = Substitute.For<IConnection>();
        newConnection.IsOpen.Returns(true);

        await holder.SetConnectionAsync(oldConnection, CancellationToken.None);
        await holder.SetConnectionAsync(newConnection, CancellationToken.None);

        await oldConnection.Received(1).DisposeAsync();
        Assert.Same(newConnection, holder.Connection);
    }

    [Fact]
    public async Task CreateChannelAsync_ThrowsInvalidOperationException_WhenNotConnected()
    {
        var holder = new RabbitMqConnectionHolder();

        await Assert.ThrowsAsync<InvalidOperationException>(() => holder.CreateChannelAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CreateChannelAsync_CreatesChannel_WhenConnected()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IChannel>();
        connection.IsOpen.Returns(true);
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>()).Returns(channel);

        await holder.SetConnectionAsync(connection, CancellationToken.None);
        var result = await holder.CreateChannelAsync(CancellationToken.None);

        Assert.Same(channel, result);
    }

    [Fact]
    public async Task DisposeAsync_DisposesConnection()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        await holder.SetConnectionAsync(connection, CancellationToken.None);
        await holder.DisposeAsync();

        await connection.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var holder = new RabbitMqConnectionHolder();
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        await holder.SetConnectionAsync(connection, CancellationToken.None);
        await holder.DisposeAsync();
        await holder.DisposeAsync();

        await connection.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task SetConnectionAsync_ThrowsObjectDisposedException_AfterDispose()
    {
        var holder = new RabbitMqConnectionHolder();
        await holder.DisposeAsync();

        var connection = Substitute.For<IConnection>();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => holder.SetConnectionAsync(connection, CancellationToken.None));
    }

    [Fact]
    public async Task SetConnectionAsync_DisposesNewConnection_WhenHolderAlreadyDisposed()
    {
        var holder = new RabbitMqConnectionHolder();
        await holder.DisposeAsync();

        var connection = Substitute.For<IConnection>();
        connection.DisposeAsync().Returns(ValueTask.CompletedTask);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => holder.SetConnectionAsync(connection, CancellationToken.None));
        await connection.Received(1).DisposeAsync();
    }
}
