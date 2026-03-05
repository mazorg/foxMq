using FoxMq.Configuration;
using FoxMq.Connection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoxMq.Tests.Connection;

public class RabbitMqConnectionInitializerTests
{
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly IOptions<RabbitMqOptions> _options;

    public RabbitMqConnectionInitializerTests()
    {
        _connectionHolder = new RabbitMqConnectionHolder();
        _options = Options.Create(new RabbitMqOptions
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        });
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectionHolderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConnectionInitializer(
            null!,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConnectionInitializer(
            _connectionHolder,
            null!,
            NullLogger<RabbitMqConnectionInitializer>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            null!));
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTask()
    {
        var initializer = new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance);

        var task = initializer.StartAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task StartedAsync_ReturnsCompletedTask()
    {
        var initializer = new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance);

        var task = initializer.StartedAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task StoppingAsync_ReturnsCompletedTask()
    {
        var initializer = new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance);

        var task = initializer.StoppingAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var initializer = new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance);

        var task = initializer.StopAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }

    [Fact]
    public async Task StoppedAsync_ReturnsCompletedTask()
    {
        var initializer = new RabbitMqConnectionInitializer(
            _connectionHolder,
            _options,
            NullLogger<RabbitMqConnectionInitializer>.Instance);

        var task = initializer.StoppedAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }
}
