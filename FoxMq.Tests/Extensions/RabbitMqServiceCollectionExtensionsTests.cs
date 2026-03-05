using FoxMq.Configuration;
using FoxMq.Connection;
using FoxMq.Consumer;
using FoxMq.Extensions;
using FoxMq.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoxMq.Tests.Extensions;

public class RabbitMqServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRabbitMq_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRabbitMq(_ => { }));
    }

    [Fact]
    public void AddRabbitMq_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddRabbitMq(null!));
    }

    [Fact]
    public void AddRabbitMq_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRabbitMq(options =>
        {
            options.HostName = "test-host";
            options.Port = 5673;
            options.UserName = "admin";
            options.Password = "secret";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        Assert.Equal("test-host", options.HostName);
        Assert.Equal(5673, options.Port);
        Assert.Equal("admin", options.UserName);
        Assert.Equal("secret", options.Password);
    }

    [Fact]
    public void AddRabbitMq_RegistersConnectionHolder_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRabbitMq(_ => { });

        var provider = services.BuildServiceProvider();
        var holder1 = provider.GetRequiredService<RabbitMqConnectionHolder>();
        var holder2 = provider.GetRequiredService<RabbitMqConnectionHolder>();

        Assert.Same(holder1, holder2);
    }

    [Fact]
    public void AddRabbitMq_RegistersConnectionInitializer_AsHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRabbitMq(_ => { });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices, s => s is RabbitMqConnectionInitializer);
    }

    [Fact]
    public void AddConsumer_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddConsumer<TestConsumer, TestMessage>(q => q.QueueName = "q"));
    }

    [Fact]
    public void AddConsumer_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddConsumer<TestConsumer, TestMessage>(null!));
    }

    [Fact]
    public void AddConsumer_ThrowsInvalidOperationException_WhenNoQueueNameConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        Assert.Throws<InvalidOperationException>(() =>
            services.AddConsumer<TestConsumer, TestMessage>(_ => { }));
    }

    [Fact]
    public void AddConsumer_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage>(config =>
        {
            config.QueueName = "test-queue";
            config.PrefetchCount = 20;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices, s => s is RabbitMqConsumerService<TestConsumer, TestMessage>);
    }

    [Fact]
    public void AddConsumer_RegistersConsumer_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage>(q => q.QueueName = "test-queue");

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TestConsumer));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddConsumer_RegistersDefaultJsonDeserializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage>(q => q.QueueName = "test-queue");

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageDeserializer<TestMessage>));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(JsonMessageDeserializer<TestMessage>), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddConsumer_DoesNotOverrideCustomDeserializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        // Register custom deserializer first
        services.AddScoped<IMessageDeserializer<TestMessage>, CustomDeserializer>();

        services.AddConsumer<TestConsumer, TestMessage>(q => q.QueueName = "test-queue");

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var deserializer = scope.ServiceProvider.GetRequiredService<IMessageDeserializer<TestMessage>>();

        Assert.IsType<CustomDeserializer>(deserializer);
    }

    [Fact]
    public void AddConsumer_CanRegisterMultipleConsumers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage>(q => q.QueueName = "test-queue");
        services.AddConsumer<AnotherTestConsumer, AnotherMessage>(q => q.QueueName = "another-queue");

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices, s => s is RabbitMqConsumerService<TestConsumer, TestMessage>);
        Assert.Contains(hostedServices, s => s is RabbitMqConsumerService<AnotherTestConsumer, AnotherMessage>);
    }

    [Fact]
    public void AddConsumerWithConfig_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg => cfg.QueueName = "q"));
    }

    [Fact]
    public void AddConsumerWithConfig_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(null!));
    }

    [Fact]
    public void AddConsumerWithConfig_ThrowsInvalidOperationException_WhenNoQueueName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        Assert.Throws<InvalidOperationException>(() =>
            services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg => { }));
    }

    [Fact]
    public void AddConsumerWithConfig_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg =>
        {
            cfg.QueueName = "quorum-queue";
            cfg.DeliveryLimit = 3;
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices,
            s => s is RabbitMqConsumerService<TestConsumer, TestMessage>);
    }

    [Fact]
    public void AddConsumerWithConfig_RegistersConsumer_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg =>
        {
            cfg.QueueName = "quorum-queue";
        });

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TestConsumer));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddConsumerWithConfig_RegistersDefaultJsonDeserializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg =>
        {
            cfg.QueueName = "quorum-queue";
        });

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageDeserializer<TestMessage>));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(JsonMessageDeserializer<TestMessage>), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddConsumerWithConfig_CanRegisterMultipleConsumers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddConsumer<TestConsumer, TestMessage, QuorumQueueConfig>(cfg =>
        {
            cfg.QueueName = "quorum-queue-1";
        });
        services.AddConsumer<AnotherTestConsumer, AnotherMessage, QuorumQueueConfig>(cfg =>
        {
            cfg.QueueName = "quorum-queue-2";
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices, s => s is RabbitMqConsumerService<TestConsumer, TestMessage>);
        Assert.Contains(hostedServices, s => s is RabbitMqConsumerService<AnotherTestConsumer, AnotherMessage>);
    }

    public record TestMessage(string Value);
    public record AnotherMessage(int Id);

    private class TestConsumer : IRabbitMqConsumer<TestMessage>
    {
        public Task HandleMessageAsync(TestMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class AnotherTestConsumer : IRabbitMqConsumer<AnotherMessage>
    {
        public Task HandleMessageAsync(AnotherMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class CustomDeserializer : IMessageDeserializer<TestMessage>
    {
        public TestMessage Deserialize(ReadOnlySpan<byte> body) => new("custom");
    }
}