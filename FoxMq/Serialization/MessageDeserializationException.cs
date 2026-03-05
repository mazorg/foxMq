namespace FoxMq.Serialization;

/// <summary>
/// Exception thrown when message deserialization fails.
/// </summary>
public sealed class MessageDeserializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused the deserialization failure.</param>
    public MessageDeserializationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
