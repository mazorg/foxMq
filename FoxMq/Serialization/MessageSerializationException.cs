namespace FoxMq.Serialization;

/// <summary>
/// Exception thrown when message serialization fails.
/// </summary>
public sealed class MessageSerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSerializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused the serialization failure.</param>
    public MessageSerializationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
