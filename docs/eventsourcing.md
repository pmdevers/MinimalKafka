## Aggregates Feature Documentation

This feature provides a lightweight, Kafka-based implementation of the *Event Sourcing* pattern for .NET applications. It offers:

- **Aggregate Definitions** via simple interfaces (`IAggregate`, `ICommand`, `IEvent`).
- **Commanders** to translate commands into events.
- **Appliers** to project events onto aggregate state.
- **In-Memory State Store** for persisting and retrieving aggregates.
- **Extension Methods** to wire up command and event handling onto an ASP.NET Core application using MinimalKafka.

---

### 1. Core Interfaces

**IAggregate** (`IAggregate.cs`)

```csharp
public interface IAggregate<TKey> where TKey : notnull
{
    TKey Key { get; init; }
}
```

- Base for all aggregate state objects, providing a strong-typed identifier.

**ICommand** (`ICommand.cs`)

```csharp
public interface ICommand<TKey> where TKey : notnull
{
    TKey Key { get; }
}
```

- Represents client intent; contains the target aggregate key.

**IEvent** (`IEvent.cs`)

```csharp
public interface IEvent<TKey> where TKey : notnull
{
    TKey Key { get; }
}
```

- Immutable facts describing state transitions; contains the aggregate key.

---

### 2. Aggregate Behavior

Aggregates encapsulate business rules by:

- **Commander** (`ICommanderBuilder`): converts a command into one or more events.
- **Applier** (`IApplierBuilder`): applies events to mutate state in an immutable fashion.

#### Sample Aggregate (`MapAggregate.cs`)

```csharp
public record Agg() : IAggregate<Guid>
{
    public Guid Key { get; init; }
    public int Version { get; init; }
    public string Name { get; private set; } = string.Empty;

    // Command handler: returns events or failure
    internal Result<Event[]> Create(Command c) { ... }

    // Event appliers: return new state or failure
    internal Result<Agg> Created(Event e) => ...;
    internal Result<Agg> Deleted(Event e) => ...;

    public static Agg Create(Guid key) => new() { Key = key };
}
```

- `AggCreate` implements command handling to produce `Event` objects.
- `AggApplier` wires up event types to the applier methods.

---

### 3. Builders & Extensions

**AggregateExtensions** (`AggregateExtensions.cs`)

- **InMemoryAggregateStore**: registers an in-memory state store `IAggregateStore<TKey, TState>`.
- **MapCommander**: maps a Kafka topic of commands to an event producer.
- **MapApplier**: maps a Kafka topic of events to state projection.

```csharp
appBuilder
  .MapCommander<MyCommander, Guid, MyState, MyCommand, MyEvent>("commands", "events")
  .MapApplier<MyApplier, Guid, MyState, MyEvent>("events");
```

These methods:

1. Build a `CommanderBuilder` or `ApplierBuilder` via static `Configure` on your domain types.
2. Compile to a processing function.
3. Wire into MinimalKafka’s `MapStream` pipeline to handle messages.

---

### 4. Startup Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.InMemoryAggregateStore();
var app = builder.Build();

app.MapAggregate2(); // registers HTTP, Kafka commander, applier, and retrieval endpoint

app.Run();
```

- Call `InMemoryAggregateStore()` in `ConfigureServices`.
- Use `MapAggregate2()` (in `MapAggregate.cs`) to wire up endpoints and streams.

---

### 5. Retrieving State

- The example exposes a GET endpoint:
  ```csharp
  GET /test/{id}
  ```
- Retrieves the current aggregate state from the store:
  ```csharp
  var result = await store.FindByIdAsync(id);
  ```

---

### 6. Result Handling

**Result** (`Result.cs`)

- Encapsulates success/failure and error messages.
- Implicitly convertible from `T` for successful operations.

```csharp
public class Result<T>
{
  public T State { get; init; }
  public bool IsSuccess { get; init; } = true;
  public string[] ErrorMessage { get; init; } = [];
  public static implicit operator Result<T>(T value) => ...;
}
```

Use `Result.Failed(...)` to return errors from commands or appliers.

---

### 7. How It Works

1. **Client** sends a HTTP POST `/command` with a payload.
2. **Producer** publishes a `Command` to the `commands` Kafka topic.
3. **Commander** consumes commands, invokes your `ICommander`, emits one or more `Event` messages to the `events` topic.
4. **Applier** consumes events, invokes your `IEventApplier`, updates aggregate state in the store.
5. **Client** can GET the current state via HTTP, reading from the in-memory store.

---

---

### 7.1 Flow Diagram

```mermaid
flowchart LR
    Client[Client] -->|HTTP POST /command| HTTP_POST_Command[/POST /command]
    HTTP_POST_Command -->|Produce| KafkaCommands[Kafka Topic: commands]
    KafkaCommands -->|Consume| Commander[Commander]
    Commander -->|Produce| KafkaEvents[Kafka Topic: events]
    KafkaEvents -->|Consume| Applier[Applier]
    Applier -->|Update| StateStore[In-Memory State Store]
    Client2[Client] -->|HTTP GET /test/{id}| HTTP_GET_State[/GET /test/{id}]
    StateStore -->|Read| HTTP_GET_State
```

---

### 8. Extending

- Replace in-memory store with a persistent `IAggregateStore` implementation (e.g., Redis, SQL).
- Add more command/event types by implementing `ITypedCommand`/`ITypedEvent`.
- Use custom serialization settings in `MinimalKafka` when mapping streams.

This documentation should help you quickly get started and extend the aggregate feature in your .NET applications using MinimalKafka.
