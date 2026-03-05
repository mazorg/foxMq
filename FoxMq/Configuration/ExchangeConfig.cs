namespace FoxMq.Configuration;

public class ExchangeConfig
{
    public string ExchangeName { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Direct;
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; }
    public IDictionary<string, object?>? Arguments { get; set; }

    public ExchangeConfig Clone() => new()
    {
        ExchangeName = ExchangeName,
        ExchangeType = ExchangeType,
        Durable = Durable,
        AutoDelete = AutoDelete,
        Arguments = Arguments is null ? null : new Dictionary<string, object?>(Arguments)
    };
}