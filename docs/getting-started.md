# Getting Started

MinimalKafka is a lightweight, extensible library for building Kafka-based applications in .NET using minimal APIs and modern C# features.

## Prerequisites

- [.NET SDK](https://dot.net)
- [Apache Kafka](https://kafka.apache.org/quickstart) running locally or accessible remotely

## Installation

Add the NuGet package to your project:

```bash
dotnet add package MinimalKafka
```

## Basic Usage

### 1. Configure Services

Register MinimalKafka in your `Program.cs`:

```csharp
using MinimalKafka;
using MinimalKafka.Attributes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(kafka =>
{
    kafka.WithConfiguration(options =>
    {
        options.Common["bootstrap.servers"] = "localhost:19092";
        options.Consumer["group.id"] = "my-app";
        options.Consumer["auto.offset.reset"] = "earliest";
    });
});
```

`WithConfiguration` accepts librdkafka configuration keys. `Common` settings apply to both the consumer and producer; `Consumer` settings apply only to the consumer. The JSON serializer is registered by default, so no serializer setup is needed for the basic example.

### 2. Define a Kafka Handler

Build the app and map a Kafka topic to a handler:

```csharp
var app = builder.Build();

app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
{
    Console.WriteLine($"Received: {key} - {value}");
});

await app.RunAsync();
```

## Advanced Usage

- **Dependency injection:** Mark parameters resolved from the DI container with `[FromServices]`.
- **Typed messages:** Use `[FromKey]` and `[FromValue]` to bind a key and value to their respective parameter types. Values other than `string` and `byte[]` use the configured topic format, which defaults to JSON.
- **Handler parameters:** `KafkaContext` and `CancellationToken` parameters are provided automatically. A handler must return `void` or `Task`.

## Example: Handler with Dependency Injection

Add this mapping before `await app.RunAsync()` in the example above:

```csharp
app.MapTopic("orders", (
    [FromServices] ILogger<Program> logger,
    [FromKey] string orderId,
    [FromValue] string order) =>
{
    logger.LogInformation("Processing order {OrderId}: {Order}", orderId, order);
});
```

## Stopping the Service

The consumer closes and leaves its consumer group when the application shuts down. MinimalKafka commits each message's offset after its handler completes successfully; it does not perform a separate final commit during shutdown. If message handling fails, the offset is not committed and the consumer service logs and propagates the failure.

## Troubleshooting

- Ensure Kafka is running and accessible at the configured `bootstrap.servers` address.
- Check logs for connection or deserialization errors.

## Further Reading

- [MinimalKafka API Reference](./api/MinimalKafka.html)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)

---

**Happy streaming with MinimalKafka!**
