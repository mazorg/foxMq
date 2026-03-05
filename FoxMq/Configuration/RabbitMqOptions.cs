namespace FoxMq.Configuration;

/// <summary>
/// Configuration options for RabbitMQ connection.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// The configuration section key used in appsettings.
    /// </summary>
    public const string SectionKey = "RabbitMq";

    /// <summary>
    /// Gets or sets the RabbitMQ host name. Defaults to "localhost".
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ port. Defaults to 5672.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the RabbitMQ user name. Defaults to "guest".
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the RabbitMQ password. Defaults to "guest".
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the RabbitMQ virtual host. Defaults to "/".
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the client-provided connection name for identification.
    /// </summary>
    public string? ClientProvidedName { get; set; }
}
