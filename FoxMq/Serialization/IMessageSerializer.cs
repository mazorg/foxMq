namespace FoxMq.Serialization;

/// <summary>
/// Interface for serializing messages to raw bytes.
/// Implement this interface to use custom serialization formats (e.g., MessagePack, Protobuf).
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes the message to bytes.
    /// </summary>
    /// <typeparam name="TMessage">The message type to serialize.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The serialized message bytes.</returns>
    /// <exception cref="MessageSerializationException">Thrown when serialization fails.</exception>
    byte[] Serialize<TMessage>(TMessage message);
}
