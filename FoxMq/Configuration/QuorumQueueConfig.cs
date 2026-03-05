namespace FoxMq.Configuration;

/// <summary>
/// Queue configuration for RabbitMQ quorum queues.
/// Enforces quorum queue requirements: durable must be true and exclusive must be false.
/// </summary>
public sealed class QuorumQueueConfig : QueueConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuorumQueueConfig"/> class
    /// with quorum queue defaults enforced.
    /// </summary>
    public QuorumQueueConfig()
    {
        Durable = true;
        Exclusive = false;
    }

    /// <summary>
    /// Gets or sets the consumer timeout. Maps to <c>x-consumer-timeout</c>.
    /// </summary>
    public TimeSpan? ConsumerTimeout { get; set; }

    /// <summary>
    /// Gets or sets the delivery limit before dead-lettering. Maps to <c>x-delivery-limit</c>.
    /// </summary>
    public int? DeliveryLimit { get; set; }

    /// <summary>
    /// Gets or sets the dead-letter exchange name. Maps to <c>x-dead-letter-exchange</c>.
    /// </summary>
    public string? DeadLetterExchange { get; set; }

    /// <summary>
    /// Gets or sets the dead-letter routing key. Maps to <c>x-dead-letter-routing-key</c>.
    /// </summary>
    public string? DeadLetterRoutingKey { get; set; }

    /// <inheritdoc />
    public override IDictionary<string, object?> BuildArguments()
    {
        if (!Durable)
            throw new InvalidOperationException(
                "Quorum queues must be durable. Do not set Durable to false.");

        if (Exclusive)
            throw new InvalidOperationException(
                "Quorum queues cannot be exclusive. Do not set Exclusive to true.");

        if (AutoDelete)
            throw new InvalidOperationException(
                "Quorum queues cannot be auto-delete. Do not set AutoDelete to true.");

        if (DeliveryLimit.HasValue && DeliveryLimit.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(DeliveryLimit),
                DeliveryLimit.Value, "DeliveryLimit must not be negative.");

        if (ConsumerTimeout.HasValue && ConsumerTimeout.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ConsumerTimeout),
                ConsumerTimeout.Value, "ConsumerTimeout must not be negative.");

        var args = base.BuildArguments();

        args["x-queue-type"] = "quorum";

        if (ConsumerTimeout.HasValue)
            args["x-consumer-timeout"] = (long)ConsumerTimeout.Value.TotalMilliseconds;

        if (DeliveryLimit.HasValue)
            args["x-delivery-limit"] = DeliveryLimit.Value;

        if (DeadLetterExchange is not null)
            args["x-dead-letter-exchange"] = DeadLetterExchange;

        if (DeadLetterRoutingKey is not null)
            args["x-dead-letter-routing-key"] = DeadLetterRoutingKey;

        return args;
    }

    /// <inheritdoc />
    public override QueueConfig Clone() => new QuorumQueueConfig
    {
        QueueName = QueueName,
        Durable = Durable,
        Exclusive = Exclusive,
        AutoDelete = AutoDelete,
        PrefetchCount = PrefetchCount,
        Arguments = Arguments is null ? null : new Dictionary<string, object?>(Arguments),
        ConsumerTimeout = ConsumerTimeout,
        DeliveryLimit = DeliveryLimit,
        DeadLetterExchange = DeadLetterExchange,
        DeadLetterRoutingKey = DeadLetterRoutingKey,
        Exchange = Exchange?.Clone(),
        RoutingKey = RoutingKey
    };
}