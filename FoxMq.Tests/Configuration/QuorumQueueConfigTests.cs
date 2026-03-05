using FoxMq.Configuration;

namespace FoxMq.Tests.Configuration;

public class QuorumQueueConfigTests
{
    [Fact]
    public void Constructor_EnforcesDurableTrue()
    {
        var config = new QuorumQueueConfig();

        Assert.True(config.Durable);
    }

    [Fact]
    public void Constructor_EnforcesExclusiveFalse()
    {
        var config = new QuorumQueueConfig();

        Assert.False(config.Exclusive);
    }

    [Fact]
    public void DefaultValues_QuorumPropertiesAreNull()
    {
        var config = new QuorumQueueConfig();

        Assert.Null(config.ConsumerTimeout);
        Assert.Null(config.DeliveryLimit);
        Assert.Null(config.DeadLetterExchange);
        Assert.Null(config.DeadLetterRoutingKey);
    }

    [Fact]
    public void BuildArguments_AlwaysIncludesQuorumQueueType()
    {
        var config = new QuorumQueueConfig();

        var args = config.BuildArguments();

        Assert.Equal("quorum", args["x-queue-type"]);
    }

    [Fact]
    public void BuildArguments_IncludesConsumerTimeout_WhenSet()
    {
        var config = new QuorumQueueConfig
        {
            ConsumerTimeout = TimeSpan.FromMinutes(5)
        };

        var args = config.BuildArguments();

        Assert.Equal(300_000L, args["x-consumer-timeout"]);
    }

    [Fact]
    public void BuildArguments_IncludesDeliveryLimit_WhenSet()
    {
        var config = new QuorumQueueConfig
        {
            DeliveryLimit = 3
        };

        var args = config.BuildArguments();

        Assert.Equal(3, args["x-delivery-limit"]);
    }

    [Fact]
    public void BuildArguments_IncludesDeadLetterExchange_WhenSet()
    {
        var config = new QuorumQueueConfig
        {
            DeadLetterExchange = "my-dlx"
        };

        var args = config.BuildArguments();

        Assert.Equal("my-dlx", args["x-dead-letter-exchange"]);
    }

    [Fact]
    public void BuildArguments_IncludesDeadLetterRoutingKey_WhenSet()
    {
        var config = new QuorumQueueConfig
        {
            DeadLetterRoutingKey = "heavy-queue"
        };

        var args = config.BuildArguments();

        Assert.Equal("heavy-queue", args["x-dead-letter-routing-key"]);
    }

    [Fact]
    public void BuildArguments_OmitsNullProperties()
    {
        var config = new QuorumQueueConfig();

        var args = config.BuildArguments();

        Assert.Single(args); // Only x-queue-type
        Assert.Equal("quorum", args["x-queue-type"]);
    }

    [Fact]
    public void BuildArguments_MergesBaseArguments()
    {
        var config = new QuorumQueueConfig
        {
            Arguments = new Dictionary<string, object?> { ["x-max-length"] = 1000 },
            DeliveryLimit = 5
        };

        var args = config.BuildArguments();

        Assert.Equal(1000, args["x-max-length"]);
        Assert.Equal(5, args["x-delivery-limit"]);
        Assert.Equal("quorum", args["x-queue-type"]);
    }

    [Fact]
    public void BuildArguments_AllPropertiesSet_ReturnsAllArguments()
    {
        var config = new QuorumQueueConfig
        {
            ConsumerTimeout = TimeSpan.FromHours(1),
            DeliveryLimit = 2,
            DeadLetterExchange = "dlx",
            DeadLetterRoutingKey = "dlq"
        };

        var args = config.BuildArguments();

        Assert.Equal("quorum", args["x-queue-type"]);
        Assert.Equal(3_600_000L, args["x-consumer-timeout"]);
        Assert.Equal(2, args["x-delivery-limit"]);
        Assert.Equal("dlx", args["x-dead-letter-exchange"]);
        Assert.Equal("dlq", args["x-dead-letter-routing-key"]);
        Assert.Equal(5, args.Count);
    }

    [Fact]
    public void BuildArguments_EmptyStringDeadLetterExchange_IsIncluded()
    {
        var config = new QuorumQueueConfig
        {
            DeadLetterExchange = ""
        };

        var args = config.BuildArguments();

        Assert.Equal("", args["x-dead-letter-exchange"]);
    }

    [Fact]
    public void BuildArguments_ThrowsInvalidOperationException_WhenDurableIsFalse()
    {
        var config = new QuorumQueueConfig { Durable = false };

        Assert.Throws<InvalidOperationException>(() => config.BuildArguments());
    }

    [Fact]
    public void BuildArguments_ThrowsInvalidOperationException_WhenExclusiveIsTrue()
    {
        var config = new QuorumQueueConfig { Exclusive = true };

        Assert.Throws<InvalidOperationException>(() => config.BuildArguments());
    }

    [Fact]
    public void BuildArguments_ThrowsInvalidOperationException_WhenAutoDeleteIsTrue()
    {
        var config = new QuorumQueueConfig { AutoDelete = true };

        Assert.Throws<InvalidOperationException>(() => config.BuildArguments());
    }

    [Fact]
    public void BuildArguments_ThrowsArgumentOutOfRangeException_WhenDeliveryLimitIsNegative()
    {
        var config = new QuorumQueueConfig { DeliveryLimit = -1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.BuildArguments());
    }

    [Fact]
    public void BuildArguments_ThrowsArgumentOutOfRangeException_WhenConsumerTimeoutIsNegative()
    {
        var config = new QuorumQueueConfig { ConsumerTimeout = TimeSpan.FromSeconds(-1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.BuildArguments());
    }

    [Fact]
    public void BuildArguments_UserSetQueueType_IsOverriddenToQuorum()
    {
        var config = new QuorumQueueConfig
        {
            Arguments = new Dictionary<string, object?> { ["x-queue-type"] = "classic" }
        };

        var args = config.BuildArguments();

        Assert.Equal("quorum", args["x-queue-type"]);
    }

    [Fact]
    public void Clone_PreservesDerivedProperties()
    {
        var config = new QuorumQueueConfig
        {
            QueueName = "test-queue",
            PrefetchCount = 10,
            ConsumerTimeout = TimeSpan.FromMinutes(5),
            DeliveryLimit = 3,
            DeadLetterExchange = "dlx",
            DeadLetterRoutingKey = "dlq",
            Arguments = new Dictionary<string, object?> { ["x-max-length"] = 1000 }
        };

        var clone = config.Clone();

        var quorumClone = Assert.IsType<QuorumQueueConfig>(clone);
        Assert.Equal("test-queue", quorumClone.QueueName);
        Assert.Equal(10, quorumClone.PrefetchCount);
        Assert.Equal(TimeSpan.FromMinutes(5), quorumClone.ConsumerTimeout);
        Assert.Equal(3, quorumClone.DeliveryLimit);
        Assert.Equal("dlx", quorumClone.DeadLetterExchange);
        Assert.Equal("dlq", quorumClone.DeadLetterRoutingKey);
        Assert.Equal(1000, quorumClone.Arguments!["x-max-length"]);
        Assert.True(quorumClone.Durable);
        Assert.False(quorumClone.Exclusive);
    }
}
