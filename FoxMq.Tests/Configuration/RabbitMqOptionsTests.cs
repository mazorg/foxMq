using FoxMq.Configuration;

namespace FoxMq.Tests.Configuration;

public class RabbitMqOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new RabbitMqOptions();

        Assert.Equal("localhost", options.HostName);
        Assert.Equal(5672, options.Port);
        Assert.Equal("guest", options.UserName);
        Assert.Equal("guest", options.Password);
        Assert.Equal("/", options.VirtualHost);
        Assert.Null(options.ClientProvidedName);
    }

    [Fact]
    public void SectionKey_IsRabbitMq()
    {
        Assert.Equal("RabbitMq", RabbitMqOptions.SectionKey);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new RabbitMqOptions
        {
            HostName = "rabbitmq.example.com",
            Port = 5673,
            UserName = "admin",
            Password = "secret",
            VirtualHost = "/production",
            ClientProvidedName = "MyApp"
        };

        Assert.Equal("rabbitmq.example.com", options.HostName);
        Assert.Equal(5673, options.Port);
        Assert.Equal("admin", options.UserName);
        Assert.Equal("secret", options.Password);
        Assert.Equal("/production", options.VirtualHost);
        Assert.Equal("MyApp", options.ClientProvidedName);
    }
}
