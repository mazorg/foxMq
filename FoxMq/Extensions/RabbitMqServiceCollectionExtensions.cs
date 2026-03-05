using FoxMq.Configuration;
using FoxMq.Connection;
using FoxMq.Consumer;
using FoxMq.Publisher;
using FoxMq.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoxMq.Extensions;

/// <summary>
/// Extension methods for registering RabbitMQ services with the dependency injection container.
/// </summary>
public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the RabbitMQ options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        Action<RabbitMqOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // Register singleton connection holder (shared across all consumers)
        services.TryAddSingleton<RabbitMqConnectionHolder>();

        // Register the connection initializer as a hosted lifecycle service
        // This ensures connection is established before consumers start
        services.AddHostedService<RabbitMqConnectionInitializer>();

        return services;
    }

    /// <summary>
    /// Registers a RabbitMQ consumer with queue configuration.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type implementing <see cref="IRabbitMqConsumer{TMessage}"/>.</typeparam>
    /// <typeparam name="TMessage">The message type the consumer handles.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure queue settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no queue name is provided.</exception>
    public static IServiceCollection AddConsumer<TConsumer, TMessage>(
        this IServiceCollection services,
        Action<QueueConfig> configure)
        where TConsumer : class, IRabbitMqConsumer<TMessage>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var queueConfig = new QueueConfig();
        configure(queueConfig);

        if (string.IsNullOrWhiteSpace(queueConfig.QueueName))
        {
            throw new InvalidOperationException(
                $"Queue name must be specified for consumer '{typeof(TConsumer).Name}'.");
        }

        RegisterConsumerService<TConsumer, TMessage>(services, queueConfig);

        return services;
    }

    /// <summary>
    /// Registers a RabbitMQ consumer with a specific queue configuration type.
    /// Use this overload to register consumers with specialized config types such as <see cref="QuorumQueueConfig"/>.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type implementing <see cref="IRabbitMqConsumer{TMessage}"/>.</typeparam>
    /// <typeparam name="TMessage">The message type the consumer handles.</typeparam>
    /// <typeparam name="TConfig">The queue configuration type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure queue settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no queue name is provided.</exception>
    public static IServiceCollection AddConsumer<TConsumer, TMessage, TConfig>(
        this IServiceCollection services,
        Action<TConfig> configure)
        where TConsumer : class, IRabbitMqConsumer<TMessage>
        where TConfig : QueueConfig, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var queueConfig = new TConfig();
        configure(queueConfig);

        if (string.IsNullOrWhiteSpace(queueConfig.QueueName))
        {
            throw new InvalidOperationException(
                $"Queue name must be specified for consumer '{typeof(TConsumer).Name}'.");
        }

        RegisterConsumerService<TConsumer, TMessage>(services, queueConfig);

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ publisher services to the service collection.
    /// Requires <see cref="AddRabbitMq"/> to be called first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the default JSON serializer if not already registered
        services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

        // Register the publisher as singleton (stateless, shares connection)
        services.TryAddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }


    private static void RegisterConsumerService<TConsumer, TMessage>(
        IServiceCollection services, QueueConfig queueConfig)
        where TConsumer : class, IRabbitMqConsumer<TMessage>
    {
        // Register the consumer as scoped (new instance per message)
        services.TryAddScoped<TConsumer>();

        // Register the default JSON deserializer for this message type if not already registered
        services.TryAddScoped<IMessageDeserializer<TMessage>, JsonMessageDeserializer<TMessage>>();

        // Register the background service with its specific queue config
        services.AddHostedService(sp =>
        {
            var connectionHolder = sp.GetRequiredService<RabbitMqConnectionHolder>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger =
                sp.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<RabbitMqConsumerService<TConsumer, TMessage>>>();

            return new RabbitMqConsumerService<TConsumer, TMessage>(
                connectionHolder,
                scopeFactory,
                queueConfig,
                logger);
        });
    }

}