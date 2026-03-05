# FoxMq

A lightweight RabbitMQ client library for .NET built on top of [RabbitMQ.Client 7.x](https://www.rabbitmq.com/client-libraries/dotnet-api-guide). FoxMq provides a convention-based consumer/publisher model with first-class dependency injection support for `Microsoft.Extensions.DependencyInjection`.

## Features

- **Typed consumers** — implement `IRabbitMqConsumer<TMessage>` and let FoxMq handle channel management, queue declaration, deserialization, ack/nack.
- **Publisher** — inject `IMessagePublisher` to publish to exchanges or directly to queues.
- **Exchange binding** — declare an exchange and bind a queue to it at startup via `ExchangeConfig`.
- **Quorum queue support** — use `QuorumQueueConfig` with built-in validation of quorum queue invariants.
- **Pluggable serialization** — System.Text.Json by default, replaceable with any format (MessagePack, Protobuf, etc.).
- **Scoped consumers** — each message is processed in its own DI scope, so scoped services work naturally.
- **Fluent configuration** — configure queues imperatively via `AddConsumer`.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\FoxMq\FoxMq.csproj" />
```

## Quick start

### 1. Configure RabbitMQ connection

In `Program.cs` or your DI setup:

```csharp
using FoxMq.Extensions;

builder.Services.AddRabbitMq(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
    options.ClientProvidedName = "my-service";
});
```

This registers:
- `RabbitMqConnectionHolder` (singleton) — thread-safe holder for the shared connection.
- `RabbitMqConnectionInitializer` (hosted lifecycle service) — establishes the connection on startup before other hosted services.

### 2. Create a consumer

Define a message type and implement `IRabbitMqConsumer<TMessage>`:

```csharp
public record OrderCreatedEvent(Guid OrderId, string CustomerName, decimal Total);

public class OrderCreatedConsumer : IRabbitMqConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleMessageAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderId} created for {Customer}", message.OrderId, message.CustomerName);
        // Process the order...
        return Task.CompletedTask;
    }
}
```

### 3. Register the consumer

```csharp
builder.Services.AddConsumer<OrderCreatedConsumer, OrderCreatedEvent>(queue =>
{
    queue.QueueName = "orders.created";
    queue.PrefetchCount = 5;
    queue.Durable = true;
});
```

That's it. FoxMq will:
- Declare the queue on startup.
- Listen for messages using `AsyncEventingBasicConsumer`.
- Deserialize each message body as `OrderCreatedEvent` using System.Text.Json.
- Resolve `OrderCreatedConsumer` from a new DI scope per message.
- Ack on success, nack + requeue on failure.

## Consumer configuration

```csharp
services.AddConsumer<MyConsumer, MyMessage>(queue =>
{
    queue.QueueName = "my-queue";
    queue.Durable = true;
    queue.Exclusive = false;
    queue.AutoDelete = false;
    queue.PrefetchCount = 10;
    queue.Arguments = new Dictionary<string, object?>
    {
        ["x-message-ttl"] = 60000
    };
});
```

All `QueueConfig` properties:

| Property | Default | Description |
|----------|---------|-------------|
| `QueueName` | `""` | Name of the queue to consume from (required) |
| `Durable` | `true` | Queue survives broker restart |
| `Exclusive` | `false` | Queue is exclusive to this connection |
| `AutoDelete` | `false` | Queue is deleted when the last consumer disconnects |
| `PrefetchCount` | `1` | Number of unacknowledged messages per consumer |
| `Arguments` | `null` | Additional queue arguments (e.g., `x-message-ttl`, `x-max-length`) |
| `Exchange` | `null` | Optional exchange to declare and bind the queue to at startup |
| `RoutingKey` | `""` | Routing key used when binding the queue to the exchange |

## Exchange binding

By default, consumers declare only a queue and consume from it directly. If messages are routed through an exchange, set `Exchange` on the queue config. FoxMq will declare the exchange and bind the queue to it before starting to consume.

```csharp
services.AddConsumer<OrderCreatedConsumer, OrderCreatedEvent>(queue =>
{
    queue.QueueName = "orders.created";
    queue.PrefetchCount = 5;
    queue.RoutingKey = "order.created";
    queue.Exchange = new ExchangeConfig
    {
        ExchangeName = "orders",
        ExchangeType = "topic",   // direct | fanout | topic | headers
        Durable = true,
    };
});
```

All `ExchangeConfig` properties:

| Property | Default | Description |
|----------|---------|-------------|
| `ExchangeName` | `""` | Name of the exchange to declare (required) |
| `ExchangeType` | `"direct"` | Exchange type: `direct`, `fanout`, `topic`, or `headers` |
| `Durable` | `true` | Exchange survives broker restart |
| `AutoDelete` | `false` | Exchange is deleted when the last binding is removed |
| `Arguments` | `null` | Additional exchange arguments |

> **Note:** When `Exchange` is `null` (the default), no exchange declaration or queue binding is performed — existing consumers are unaffected.

## Quorum queues

Use the `AddConsumer<TConsumer, TMessage, TConfig>` overload with `QuorumQueueConfig` for quorum queue support:

```csharp
services.AddConsumer<MyConsumer, MyMessage, QuorumQueueConfig>(queue =>
{
    queue.QueueName = "my-quorum-queue";
    queue.PrefetchCount = 5;
    queue.DeliveryLimit = 3;
    queue.DeadLetterExchange = "dlx";
    queue.DeadLetterRoutingKey = "dlq.my-queue";
    queue.ConsumerTimeout = TimeSpan.FromMinutes(5);
});
```

`QuorumQueueConfig` enforces quorum queue constraints at configuration time:
- `Durable` must be `true` (default).
- `Exclusive` must be `false` (default).
- `AutoDelete` must be `false` (default).

It also sets `x-queue-type = "quorum"` and maps `DeliveryLimit`, `DeadLetterExchange`, `DeadLetterRoutingKey`, and `ConsumerTimeout` to their respective RabbitMQ arguments.

## Publishing messages

### Setup

```csharp
builder.Services.AddRabbitMq(options => { /* ... */ });
builder.Services.AddRabbitMqPublisher();
```

### Publish to an exchange

```csharp
public class OrderService
{
    private readonly IMessagePublisher _publisher;

    public OrderService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PlaceOrderAsync(Order order, CancellationToken ct)
    {
        // ... save order ...

        await _publisher.PublishAsync(
            new OrderCreatedEvent(order.Id, order.CustomerName, order.Total),
            exchange: "orders",
            routingKey: "order.created",
            ct);
    }
}
```

### Publish directly to a queue

```csharp
await _publisher.PublishToQueueAsync(myMessage, queueName: "my-queue", ct);
```

This uses the default exchange (`""`) with the queue name as the routing key.

### Typed publisher extensions

You can create extension methods on `IMessagePublisher` that pre-bind the exchange/routing key or queue name, giving callers a strongly-typed API with no magic strings:

```csharp
public static class OrderPublisherExtensions
{
    public static Task PublishOrderCreatedAsync(
        this IMessagePublisher publisher,
        OrderCreatedEvent message,
        CancellationToken ct = default)
        => publisher.PublishAsync(message, exchange: "orders", routingKey: "order.created", ct);

    public static Task PublishOrderCancelledAsync(
        this IMessagePublisher publisher,
        OrderCancelledEvent message,
        CancellationToken ct = default)
        => publisher.PublishToQueueAsync(message, queueName: "orders.cancelled", ct);
}
```

Callers then use the typed methods directly:

```csharp
await _publisher.PublishOrderCreatedAsync(orderEvent, ct);
```

## Custom serialization

FoxMq uses System.Text.Json by default. To customize the JSON options:

```csharp
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = false;
});
```

To replace JSON entirely (e.g., with MessagePack), implement the serialization interfaces and register them before calling `AddRabbitMqPublisher` / `AddConsumer`:

```csharp
// Publisher serialization
public class MessagePackSerializer : IMessageSerializer
{
    public byte[] Serialize<TMessage>(TMessage message)
    {
        // Your serialization logic
    }
}

// Consumer deserialization (per message type)
public class MessagePackDeserializer<TMessage> : IMessageDeserializer<TMessage>
{
    public TMessage Deserialize(ReadOnlySpan<byte> body)
    {
        // Your deserialization logic
    }
}

// Register before AddRabbitMqPublisher / AddConsumer:
services.AddSingleton<IMessageSerializer, MessagePackSerializer>();
services.AddScoped(typeof(IMessageDeserializer<>), typeof(MessagePackDeserializer<>));
```

FoxMq uses `TryAdd*` internally, so registrations made before `AddConsumer` / `AddRabbitMqPublisher` take precedence.

## Message handling behavior

| Outcome | Action |
|---------|--------|
| `HandleMessageAsync` completes successfully | Message is **ack'd** |
| `HandleMessageAsync` throws an exception | Message is **nack'd + requeued** |
| Deserialization fails (`MessageDeserializationException`) | Message is **nack'd + requeued** |

Each message is processed in its own `IServiceScope`, so scoped services (e.g., `DbContext`) are isolated per message.

## Connection lifecycle

1. `RabbitMqConnectionInitializer` implements `IHostedLifecycleService` and connects during the `StartingAsync` phase — before other hosted services start.
2. `RabbitMqConnectionHolder` is a singleton that holds the shared `IConnection` and provides `CreateChannelAsync` for consumers and the publisher.
3. On shutdown, the connection holder disposes the connection.

## Architecture overview

```
┌─────────────────────────────────────────────────────┐
│                  Your Application                   │
│                                                     │
│  ┌────────────────────┐    ┌──────────────────────┐ │
│  │  IMessagePublisher │    │ IRabbitMqConsumer<T> │ │
│  │  (publish msgs)    │    │  (handle msgs)       │ │
│  └────────┬───────────┘    └──────────┬───────────┘ │
│           │                           │             │
│  ┌────────▼──────────┐    ┌───────────▼───────────┐ │
│  │ RabbitMqPublisher │    │ RabbitMqConsumerSvc   │ │
│  │  (channel mgmt)   │    │ (channel, qos, loop)  │ │
│  └─────────┬─────────┘    └──────────┬────────────┘ │
│            │                         │              │
│  ┌─────────▼─────────────────────────▼────────────┐ │
│  │          RabbitMqConnectionHolder              │ │
│  │          (shared IConnection)                  │ │
│  └──────────────────┬─────────────────────────────┘ │
│                     │                               │
│  ┌──────────────────▼─────────────────────────────┐ │
│  │       RabbitMqConnectionInitializer            │ │
│  │        (IHostedLifecycleService)               │ │
│  └────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
                      │
                      ▼
              ┌───────────────┐
              │   RabbitMQ    │
              │   Broker      │
              └───────────────┘
```

## Full example

```csharp
var builder = WebApplication.CreateBuilder(args);

// RabbitMQ connection
builder.Services.AddRabbitMq(options =>
{
    options.HostName = "rabbitmq.internal";
    options.Port = 5672;
    options.UserName = "app-user";
    options.Password = "secret";
    options.VirtualHost = "production";
    options.ClientProvidedName = "order-service";
});

// Publisher
builder.Services.AddRabbitMqPublisher();

// Consumers
builder.Services.AddConsumer<OrderCreatedConsumer, OrderCreatedEvent>(q =>
{
    q.QueueName = "orders.created";
    q.PrefetchCount = 10;
});

builder.Services.AddConsumer<PaymentProcessedConsumer, PaymentProcessedEvent, QuorumQueueConfig>(q =>
{
    q.QueueName = "payments.processed";
    q.PrefetchCount = 5;
    q.DeliveryLimit = 3;
    q.DeadLetterExchange = "dlx";
});

var app = builder.Build();
app.Run();
```

## Dependencies

| Package | Version |
|---------|---------|
| `RabbitMQ.Client` | 7.2.0 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.2 |
| `Microsoft.Extensions.Hosting.Abstractions` | 10.0.2 |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.2 |
| `Microsoft.Extensions.Options` | 10.0.2 |