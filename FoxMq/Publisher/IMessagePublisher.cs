namespace FoxMq.Publisher;

/// <summary>
/// Interface for publishing messages to RabbitMQ exchanges and queues.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to an exchange with the specified routing key.
    /// </summary>
    /// <typeparam name="TMessage">The message type to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="exchange">The exchange name to publish to.</param>
    /// <param name="routingKey">The routing key for the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TMessage>(TMessage message, string exchange, string routingKey,
        CancellationToken cancellationToken = default);

    Task PublishAsync<TMessage>(TMessage message, string exchange, CancellationToken cancellationToken = default) =>
        PublishAsync(message, exchange, "#", cancellationToken);


    /// <summary>
    /// Publishes a message directly to a queue using the default exchange.
    /// </summary>
    /// <typeparam name="TMessage">The message type to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="queueName">The queue name to publish to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishToQueueAsync<TMessage>(TMessage message, string queueName,
        CancellationToken cancellationToken = default);
}