namespace FoxMq.Configuration;

/// <summary>
/// Configuration settings for a RabbitMQ queue and its consumer.
/// </summary>
public class QueueConfig
{
    /// <summary>
    /// Gets or sets the name of the queue to consume from. Required.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the queue should survive a broker restart. Defaults to true.
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the queue is exclusive to this connection. Defaults to false.
    /// </summary>
    public bool Exclusive { get; set; }

    /// <summary>
    /// Gets or sets whether the queue should be deleted when the last consumer disconnects. Defaults to false.
    /// </summary>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets the prefetch count (number of unacknowledged messages per consumer). Defaults to 1.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets additional queue arguments (e.g., x-message-ttl, x-max-length).
    /// Subclasses (such as <see cref="QuorumQueueConfig"/>) may add or override entries
    /// via <see cref="BuildArguments"/>.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; set; }

    public ExchangeConfig? Exchange { get; set; }
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Assembles the queue arguments dictionary. Override in subclasses to add custom arguments.
    /// </summary>
    public virtual IDictionary<string, object?> BuildArguments() =>
        Arguments is not null
            ? new Dictionary<string, object?>(Arguments)
            : new Dictionary<string, object?>();

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    public virtual QueueConfig Clone() => new()
    {
        QueueName = QueueName,
        Durable = Durable,
        Exclusive = Exclusive,
        AutoDelete = AutoDelete,
        PrefetchCount = PrefetchCount,
        Arguments = Arguments is null ? null : new Dictionary<string, object?>(Arguments),
        Exchange = Exchange?.Clone(),
        RoutingKey = RoutingKey
    };
}