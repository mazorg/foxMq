using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FoxMq.Serialization;

/// <summary>
/// Default message deserializer using System.Text.Json.
/// </summary>
/// <typeparam name="TMessage">The message type to deserialize to.</typeparam>
public sealed class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
{
    private readonly JsonSerializerOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMessageDeserializer{TMessage}"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options. If not provided, uses default options.</param>
    public JsonMessageDeserializer(IOptions<JsonSerializerOptions>? options = null)
    {
        _options = options?.Value;
    }

    /// <inheritdoc />
    public TMessage Deserialize(ReadOnlySpan<byte> body)
    {
        try
        {
            var result = JsonSerializer.Deserialize<TMessage>(body, _options);

            if (result is null)
            {
                throw new MessageDeserializationException(
                    $"Deserialization of {typeof(TMessage).Name} returned null.");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new MessageDeserializationException(
                $"Failed to deserialize message to {typeof(TMessage).Name}: {ex.Message}", ex);
        }
    }
}
