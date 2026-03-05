namespace FoxMq.Serialization;

/// <summary>
/// Interface for deserializing messages from raw bytes.
/// Implement this interface to use custom serialization formats (e.g., MessagePack, Protobuf).
/// </summary>
/// <typeparam name="TMessage">The message type to deserialize to.</typeparam>
public interface IMessageDeserializer<out TMessage>
{
    /// <summary>
    /// Deserializes the message body to the specified type.
    /// </summary>
    /// <param name="body">The raw message bytes.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="MessageDeserializationException">Thrown when deserialization fails.</exception>
    TMessage Deserialize(ReadOnlySpan<byte> body);
}
