using System.Text;
using System.Text.Json;
using FoxMq.Serialization;
using Microsoft.Extensions.Options;

namespace FoxMq.Tests.Serialization;

public class JsonMessageDeserializerTests
{
    [Fact]
    public void Deserialize_ValidJson_ReturnsDeserializedObject()
    {
        var deserializer = new JsonMessageDeserializer<TestMessage>();
        var json = """{"Name":"Test","Value":42}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = deserializer.Deserialize(bytes);

        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Deserialize_WithCustomOptions_UsesOptions()
    {
        var options = Options.Create(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var deserializer = new JsonMessageDeserializer<TestMessage>(options);
        var json = """{"name":"Test","value":42}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = deserializer.Deserialize(bytes);

        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsMessageDeserializationException()
    {
        var deserializer = new JsonMessageDeserializer<TestMessage>();
        var invalidJson = "not valid json";
        var bytes = Encoding.UTF8.GetBytes(invalidJson);

        var ex = Assert.Throws<MessageDeserializationException>(() => deserializer.Deserialize(bytes));
        Assert.Contains("Failed to deserialize", ex.Message);
        Assert.Contains(nameof(TestMessage), ex.Message);
        Assert.IsType<JsonException>(ex.InnerException);
    }

    [Fact]
    public void Deserialize_NullResult_ThrowsMessageDeserializationException()
    {
        var deserializer = new JsonMessageDeserializer<TestMessage?>();
        var json = "null";
        var bytes = Encoding.UTF8.GetBytes(json);

        var ex = Assert.Throws<MessageDeserializationException>(() => deserializer.Deserialize(bytes));
        Assert.Contains("returned null", ex.Message);
    }

    [Fact]
    public void Deserialize_EmptyBytes_ThrowsMessageDeserializationException()
    {
        var deserializer = new JsonMessageDeserializer<TestMessage>();
        var bytes = Array.Empty<byte>();

        Assert.Throws<MessageDeserializationException>(() => deserializer.Deserialize(bytes));
    }

    [Fact]
    public void Deserialize_RecordType_Works()
    {
        var deserializer = new JsonMessageDeserializer<TestRecord>();
        var json = """{"Id":123,"Description":"A record"}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = deserializer.Deserialize(bytes);

        Assert.Equal(123, result.Id);
        Assert.Equal("A record", result.Description);
    }

    [Fact]
    public void Deserialize_WithNullOptions_UsesDefaults()
    {
        var deserializer = new JsonMessageDeserializer<TestMessage>(null);
        var json = """{"Name":"Test","Value":42}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = deserializer.Deserialize(bytes);

        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    public class TestMessage
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public record TestRecord(int Id, string Description);
}
