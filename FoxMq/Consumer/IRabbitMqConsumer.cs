namespace FoxMq.Consumer;

/// <summary>
/// Interface for RabbitMQ message consumers with typed messages.
/// Implement this interface to handle messages from a queue.
/// </summary>
/// <typeparam name="TMessage">The type of message this consumer handles.</typeparam>
public interface IRabbitMqConsumer<in TMessage>
{
    /// <summary>
    /// Handles an incoming message from the queue.
    /// </summary>
    /// <param name="message">The deserialized message.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// On successful completion (no exception), the message is automatically acknowledged.
    /// </para>
    /// <para>
    /// On exception, the message is negatively acknowledged and requeued.
    /// </para>
    /// <para>
    /// Each message is processed in its own dependency injection scope,
    /// allowing scoped services to be injected into the consumer.
    /// </para>
    /// <para>
    /// By default, messages are deserialized using System.Text.Json. To use a custom
    /// deserializer, register an implementation of <see cref="FoxMq.Serialization.IMessageDeserializer{TMessage}"/>
    /// in the DI container.
    /// </para>
    /// </remarks>
    Task HandleMessageAsync(TMessage message, CancellationToken cancellationToken);
}
