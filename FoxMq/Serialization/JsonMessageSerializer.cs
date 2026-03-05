using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FoxMq.Serialization;

/// <summary>
/// Default message serializer using System.Text.Json.
/// </summary>
public sealed class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMessageSerializer"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options. If not provided, uses default options.</param>
    public JsonMessageSerializer(IOptions<JsonSerializerOptions>? options = null)
    {
        _options = options?.Value;
    }

    /// <inheritdoc />
    public byte[] Serialize<TMessage>(TMessage message)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(message, _options);
        }
        catch (JsonException ex)
        {
            throw new MessageSerializationException(
                $"Failed to serialize message of type {typeof(TMessage).Name}: {ex.Message}", ex);
        }
    }
}
