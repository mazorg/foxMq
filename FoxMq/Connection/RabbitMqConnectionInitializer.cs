using FoxMq.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FoxMq.Connection;

/// <summary>
/// Hosted service that initializes the RabbitMQ connection on application startup.
/// Runs before other hosted services to ensure connection is available for consumers.
/// </summary>
public sealed class RabbitMqConnectionInitializer : IHostedLifecycleService
{
    private readonly RabbitMqConnectionHolder _connectionHolder;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqConnectionInitializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConnectionInitializer"/> class.
    /// </summary>
    public RabbitMqConnectionInitializer(
        RabbitMqConnectionHolder connectionHolder,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionInitializer> logger)
    {
        _connectionHolder = connectionHolder ?? throw new ArgumentNullException(nameof(connectionHolder));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        _logger.LogInformation(
            "Connecting to RabbitMQ at {Host}:{Port}/{VirtualHost}",
            opts.HostName, opts.Port, opts.VirtualHost);

        var factory = new ConnectionFactory
        {
            HostName = opts.HostName,
            Port = opts.Port,
            UserName = opts.UserName,
            Password = opts.Password,
            VirtualHost = opts.VirtualHost,
            ClientProvidedName = opts.ClientProvidedName
        };

        var connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        await _connectionHolder.SetConnectionAsync(connection, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully connected to RabbitMQ");
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
