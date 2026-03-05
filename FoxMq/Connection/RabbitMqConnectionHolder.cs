using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace FoxMq.Connection;

/// <summary>
/// Thread-safe holder for the shared RabbitMQ connection.
/// </summary>
public sealed class RabbitMqConnectionHolder : IAsyncDisposable
{
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether a connection has been established.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_connection))]
    public bool IsConnected => _connection is { IsOpen: true };

    /// <summary>
    /// Gets the current connection. Throws if not connected.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when connection is not established.</exception>
    public IConnection Connection => _connection ?? throw new InvalidOperationException(
        "RabbitMQ connection has not been established. Ensure RabbitMqConnectionInitializer has started.");

    /// <summary>
    /// Sets the connection. Should only be called by RabbitMqConnectionInitializer.
    /// </summary>
    internal async Task SetConnectionAsync(IConnection connection, CancellationToken cancellationToken)
    {
        // Check disposed early before acquiring lock (lock may be disposed)
        if (_disposed)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new ObjectDisposedException(nameof(RabbitMqConnectionHolder));
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_disposed)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw new ObjectDisposedException(nameof(RabbitMqConnectionHolder));
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }

            _connection = connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Creates a new channel from the connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new channel.</returns>
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("RabbitMQ connection is not available.");
        }

        return await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
