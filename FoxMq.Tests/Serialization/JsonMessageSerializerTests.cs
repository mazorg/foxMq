using System.Text;
using System.Text.Json;
using FoxMq.Serialization;
using Microsoft.Extensions.Options;

namespace FoxMq.Tests.Serialization;

public class JsonMessageSerializerTests
{
    [Fact]
    public void Serialize_ValidObject_ReturnsJsonBytes()
    {
        var serializer = new JsonMessageSerializer();
        var message = new TestMessage { Name = "Test", Value = 42 };

        var bytes = serializer.Serialize(message);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"Name\":\"Test\"", json);
        Assert.Contains("\"Value\":42", json);
    }

    [Fact]
    public void Serialize_WithCustomOptions_UsesOptions()
    {
        var options = Options.Create(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var serializer = new JsonMessageSerializer(options);
        var message = new TestMessage { Name = "Test", Value = 42 };

        var bytes = serializer.Serialize(message);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"name\":\"Test\"", json);
        Assert.Contains("\"value\":42", json);
    }

    [Fact]
    public void Serialize_WithNullOptions_UsesDefaults()
    {
        var serializer = new JsonMessageSerializer(null);
        var message = new TestMessage { Name = "Test", Value = 42 };

        var bytes = serializer.Serialize(message);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"Name\":\"Test\"", json);
        Assert.Contains("\"Value\":42", json);
    }

    [Fact]
    public void Serialize_RecordType_Works()
    {
        var serializer = new JsonMessageSerializer();
        var message = new TestRecord(123, "A record");

        var bytes = serializer.Serialize(message);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"Id\":123", json);
        Assert.Contains("\"Description\":\"A record\"", json);
    }

    [Fact]
    public void Serialize_NullValue_ReturnsNullJson()
    {
        var serializer = new JsonMessageSerializer();

        var bytes = serializer.Serialize<TestMessage?>(null);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        var serializer = new JsonMessageSerializer();
        var deserializer = new JsonMessageDeserializer<TestMessage>();
        var original = new TestMessage { Name = "RoundTrip", Value = 99 };

        var bytes = serializer.Serialize(original);
        var result = deserializer.Deserialize(bytes);

        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Value, result.Value);
    }

    [Fact]
    public void Serialize_EmptyObject_ReturnsValidJson()
    {
        var serializer = new JsonMessageSerializer();
        var message = new TestMessage();

        var bytes = serializer.Serialize(message);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"Name\":\"\"", json);
        Assert.Contains("\"Value\":0", json);
    }

    public class TestMessage
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public record TestRecord(int Id, string Description);
}
