using FoxMq.Configuration;

namespace FoxMq.Tests.Configuration;

public class QueueConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new QueueConfig();

        Assert.Equal(string.Empty, config.QueueName);
        Assert.True(config.Durable);
        Assert.False(config.Exclusive);
        Assert.False(config.AutoDelete);
        Assert.Equal(1, config.PrefetchCount);
        Assert.Null(config.Arguments);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new QueueConfig
        {
            QueueName = "test-queue",
            Durable = false,
            Exclusive = true,
            AutoDelete = true,
            PrefetchCount = 10,
            Arguments = new Dictionary<string, object?> { ["x-message-ttl"] = 60000 }
        };

        var clone = original.Clone();

        Assert.Equal(original.QueueName, clone.QueueName);
        Assert.Equal(original.Durable, clone.Durable);
        Assert.Equal(original.Exclusive, clone.Exclusive);
        Assert.Equal(original.AutoDelete, clone.AutoDelete);
        Assert.Equal(original.PrefetchCount, clone.PrefetchCount);
        Assert.NotSame(original.Arguments, clone.Arguments);
        Assert.Equal(original.Arguments, clone.Arguments);
    }

    [Fact]
    public void Clone_WithNullArguments_ReturnsNullArguments()
    {
        var original = new QueueConfig { QueueName = "test", Arguments = null };

        var clone = original.Clone();

        Assert.Null(clone.Arguments);
    }

    [Fact]
    public void BuildArguments_WhenArgumentsIsNull_ReturnsEmptyDictionary()
    {
        var config = new QueueConfig { Arguments = null };

        var result = config.BuildArguments();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildArguments_WhenArgumentsProvided_ReturnsCopyOfArguments()
    {
        var config = new QueueConfig
        {
            Arguments = new Dictionary<string, object?> { ["x-message-ttl"] = 60000 }
        };

        var result = config.BuildArguments();

        Assert.Single(result);
        Assert.Equal(60000, result["x-message-ttl"]);
        Assert.NotSame(config.Arguments, result);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var original = new QueueConfig
        {
            QueueName = "original",
            Arguments = new Dictionary<string, object?> { ["key"] = "value" }
        };

        var clone = original.Clone();
        clone.QueueName = "modified";
        clone.Arguments!["key"] = "modified-value";
        clone.Arguments["new-key"] = "new-value";

        Assert.Equal("original", original.QueueName);
        Assert.Equal("value", original.Arguments!["key"]);
        Assert.False(original.Arguments.ContainsKey("new-key"));
    }

    [Fact]
    public void Exchange_DefaultsToNull_AndRoutingKey_DefaultsToEmpty()
    {
        var config = new QueueConfig();

        Assert.Null(config.Exchange);
        Assert.Equal(string.Empty, config.RoutingKey);
    }

    [Fact]
    public void Clone_DeepCopies_NonNullExchange()
    {
        var original = new QueueConfig
        {
            Exchange = new ExchangeConfig { ExchangeName = "test-exchange", ExchangeType = "topic" },
            RoutingKey = "order.#"
        };

        var clone = original.Clone();

        Assert.NotNull(clone.Exchange);
        Assert.NotSame(original.Exchange, clone.Exchange);
        Assert.Equal(original.Exchange.ExchangeName, clone.Exchange.ExchangeName);
        Assert.Equal(original.Exchange.ExchangeType, clone.Exchange.ExchangeType);
        Assert.Equal(original.RoutingKey, clone.RoutingKey);
    }

    [Fact]
    public void Clone_KeepsExchangeNull_WhenNull()
    {
        var original = new QueueConfig { Exchange = null };

        var clone = original.Clone();

        Assert.Null(clone.Exchange);
    }

    [Fact]
    public void Clone_MutatingClonedExchange_DoesNotAffectOriginal()
    {
        var original = new QueueConfig
        {
            Exchange = new ExchangeConfig { ExchangeName = "original-exchange" }
        };

        var clone = original.Clone();
        clone.Exchange!.ExchangeName = "modified-exchange";

        Assert.Equal("original-exchange", original.Exchange.ExchangeName);
    }
}
