using FoxMq.Configuration;

namespace FoxMq.Tests.Configuration;

public class ExchangeConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new ExchangeConfig();

        Assert.Equal(string.Empty, config.ExchangeName);
        Assert.Equal("direct", config.ExchangeType);
        Assert.True(config.Durable);
        Assert.False(config.AutoDelete);
        Assert.Null(config.Arguments);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy_WithSameValues()
    {
        var original = new ExchangeConfig
        {
            ExchangeName = "my-exchange",
            ExchangeType = "topic",
            Durable = false,
            AutoDelete = true,
            Arguments = new Dictionary<string, object?> { ["x-custom"] = 42 }
        };

        var clone = original.Clone();

        Assert.Equal(original.ExchangeName, clone.ExchangeName);
        Assert.Equal(original.ExchangeType, clone.ExchangeType);
        Assert.Equal(original.Durable, clone.Durable);
        Assert.Equal(original.AutoDelete, clone.AutoDelete);
        Assert.NotSame(original.Arguments, clone.Arguments);
        Assert.Equal(original.Arguments, clone.Arguments);
    }

    [Fact]
    public void Clone_WithNullArguments_ReturnsNullArguments()
    {
        var original = new ExchangeConfig { Arguments = null };

        var clone = original.Clone();

        Assert.Null(clone.Arguments);
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotAffectOriginal()
    {
        var original = new ExchangeConfig
        {
            ExchangeName = "original",
            Arguments = new Dictionary<string, object?> { ["key"] = "value" }
        };

        var clone = original.Clone();
        clone.ExchangeName = "modified";
        clone.Arguments!["key"] = "modified-value";
        clone.Arguments["new-key"] = "new-value";

        Assert.Equal("original", original.ExchangeName);
        Assert.Equal("value", original.Arguments!["key"]);
        Assert.False(original.Arguments.ContainsKey("new-key"));
    }
}
