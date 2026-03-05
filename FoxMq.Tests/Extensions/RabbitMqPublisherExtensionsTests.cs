using FoxMq.Extensions;
using FoxMq.Publisher;
using FoxMq.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace FoxMq.Tests.Extensions;

public class RabbitMqPublisherExtensionsTests
{
    [Fact]
    public void AddRabbitMqPublisher_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRabbitMqPublisher());
    }

    [Fact]
    public void AddRabbitMqPublisher_RegistersMessageSerializer_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddRabbitMqPublisher();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageSerializer));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
        Assert.Equal(typeof(JsonMessageSerializer), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRabbitMqPublisher_RegistersMessagePublisher_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddRabbitMqPublisher();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessagePublisher));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
        Assert.Equal(typeof(RabbitMqPublisher), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRabbitMqPublisher_ReturnsSameSerializerInstance()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });
        services.AddRabbitMqPublisher();

        var provider = services.BuildServiceProvider();
        var serializer1 = provider.GetRequiredService<IMessageSerializer>();
        var serializer2 = provider.GetRequiredService<IMessageSerializer>();

        Assert.Same(serializer1, serializer2);
    }

    [Fact]
    public void AddRabbitMqPublisher_ReturnsSamePublisherInstance()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });
        services.AddRabbitMqPublisher();

        var provider = services.BuildServiceProvider();
        var publisher1 = provider.GetRequiredService<IMessagePublisher>();
        var publisher2 = provider.GetRequiredService<IMessagePublisher>();

        Assert.Same(publisher1, publisher2);
    }

    [Fact]
    public void AddRabbitMqPublisher_DoesNotOverrideCustomSerializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        // Register custom serializer first
        services.AddSingleton<IMessageSerializer, CustomSerializer>();

        services.AddRabbitMqPublisher();

        var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IMessageSerializer>();

        Assert.IsType<CustomSerializer>(serializer);
    }

    [Fact]
    public void AddRabbitMqPublisher_DoesNotOverrideCustomPublisher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        // Register custom publisher first
        services.AddSingleton<IMessagePublisher, CustomPublisher>();

        services.AddRabbitMqPublisher();

        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        Assert.IsType<CustomPublisher>(publisher);
    }

    [Fact]
    public void AddRabbitMqPublisher_CanBeCalledMultipleTimes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        services.AddRabbitMqPublisher();
        services.AddRabbitMqPublisher();
        services.AddRabbitMqPublisher();

        var serializerDescriptors = services.Where(d => d.ServiceType == typeof(IMessageSerializer)).ToList();
        var publisherDescriptors = services.Where(d => d.ServiceType == typeof(IMessagePublisher)).ToList();

        Assert.Single(serializerDescriptors);
        Assert.Single(publisherDescriptors);
    }

    [Fact]
    public void AddRabbitMqPublisher_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMq(_ => { });

        var result = services.AddRabbitMqPublisher();

        Assert.Same(services, result);
    }

    private class CustomSerializer : IMessageSerializer
    {
        public byte[] Serialize<TMessage>(TMessage message) => [];
    }

    private class CustomPublisher : IMessagePublisher
    {
        public Task PublishAsync<TMessage>(TMessage message, string exchange, string routingKey, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishToQueueAsync<TMessage>(TMessage message, string queueName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
