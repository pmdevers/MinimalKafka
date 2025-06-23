# Getting Started

MinimalKafka is a lightweight, extensible library for building Kafka-based applications in .NET 8 using minimal APIs and modern C# features.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Apache Kafka](https://kafka.apache.org/quickstart) running locally or accessible remotely

## Installation

Add the NuGet package to your project:

```bash
dotnet add package MinimalKafka
```

## Basic Usage

### 1. Configure Services

Register MinimalKafka in your `Program.cs` or `Startup.cs`:

```csharp
using MinimalKafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(options => 
{ 
	options.WithBootstrapServers("localhost:19092");
	options.WithJsonSerializers();
});
```

### 2. Define a Kafka Handler

Map a topic to output to console

```csharp

var app = builder.Build();

app.MapTopic("my-topic", (string key, string value) =>
{
    Console.WriteLine($"Received: {key} - {value}");
});

await app.RunAsync()
```

## Advanced Usage

- **Dependency Injection:** Handlers can accept service from the DI container as parameters.
- **Strongly Typed:** You can use types for key/value.

## Example: Handler with Dependency Injection

```csharp
app.MapTopic("orders", (
	[FromServices] ILogger<Order> logger,
	[FromKey] OrderId orderId, 
	[FromValue] Order order) =>
{
     logger.LogInformation("Processing order {OrderId}: {@Order}", orderId, order);
});
```

## Stopping the Service

The Kafka service will gracefully stop and commit offsets when your application shuts down.

## Troubleshooting

- Ensure Kafka is running and accessible at the configured `BootstrapServers`.
- Check logs for connection or deserialization errors.

## Further Reading

- [MinimalKafka API Reference](./api/MinimalKafka.html)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)

---

**Happy streaming with MinimalKafka!**