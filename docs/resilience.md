# Resilience in MinimalKafka

MinimalKafka provides two opt-in, per-topic consumer policies in `MinimalKafka.Middleware.Resilience`:

- `WithRetry()` retries the downstream consumer pipeline with configurable backoff and exception filtering.
- `WithDeadLetter()` publishes failed messages to a dead-letter topic so consumption can continue after a successful publication.

Neither policy is enabled automatically. There are no built-in circuit-breaker, timeout, bulkhead, jitter, or durable retry-topic policies. Kafka client delivery retries are separate from consumer handler retries.

For dead-letter metadata, destination naming, and current payload limitations, see [Dead-Letter Queues](dead-letter-queues.md).

## Quick start

This minimal application deliberately fails each message to demonstrate retries followed by dead-letter routing. It uses raw message bytes through `KafkaContext`, so it does not need a message serializer for the handler.

```csharp
using MinimalKafka;
using MinimalKafka.Middleware.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
	config.WithConfiguration(builder.Configuration.GetSection("Kafka")));

var app = builder.Build();

app.MapTopic("retry", (KafkaContext context) =>
	Task.FromException(new InvalidOperationException($"Cannot process {context.Topic}.")))
	.WithDeadLetter()
	.WithRetry();

await app.RunAsync();
```

Configure the application's Kafka connection and consumer group, for example:

```json
{
  "Kafka": {
	"Common": {
	  "bootstrap.servers": "localhost:19092"
	},
	"Consumer": {
	  "group.id": "resilience-example",
	  "auto.offset.reset": "earliest"
	}
  }
}
```

With the defaults, the handler runs up to three times, waiting one second and then two seconds between attempts. If every attempt fails, the outer dead-letter middleware attempts to publish the failed message. See the [current DLQ limitations](dead-letter-queues.md#current-implementation-limitations) before using this path for recovery.

The repository's [retry example](../examples/Examples/Features/Resilience/Retry.cs) similarly throws on every invocation. Its registration in [FeatureExtentions.cs](../examples/Examples/Features/FeatureExtentions.cs) uses `.WithDeadLetter().WithRetry()`; the example does not provide an automatic recovery or replay handler.

## Middleware order

Register dead-letter routing **before** retries:

```text
Global consumer middleware
  -> DeadLetter
	-> Retry
	  -> Remaining topic middleware
		-> Handler
```

Middleware runs in registration order on entry. Each policy wraps the rest of the pipeline:

- `WithDeadLetter().WithRetry()` retries processing first, then dead-letters the final failure.
- `WithRetry().WithDeadLetter()` lets dead-letter routing handle a processing failure before retry sees it. A successful DLQ publication therefore prevents handler retries; a failed DLQ publication can itself trigger a retry of the downstream pipeline.
- `WithRetry()` alone propagates an exhausted or non-retryable failure.
- `WithDeadLetter()` alone routes a downstream failure without retrying it.

Only middleware registered after a policy, and the handler beneath it, are inside that policy's protection. Middleware construction, subscription, `Consume`, and offset commits are outside the topic pipeline.

## Retry options

`WithRetry(Action<RetryOptions>? configure = null)` creates options for that topic and returns its `TopicBuilder`.

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxAttempts` | `3` | Total invocations, including the initial attempt; not the number of extra retries. |
| `Delay` | `TimeSpan.FromSeconds(1)` | Initial wait after a retryable failure. |
| `BackoffMultiplier` | `2.0` | Multiplier used to calculate the next wait. |
| `MaxDelay` | `TimeSpan.FromMinutes(1)` | Cap applied when calculating subsequent waits. |
| `ShouldRetry` | `null` | Exception predicate; `null` retries every caught exception unless the context cancellation token is canceled. |

`MaxAttempts = 1` invokes the downstream pipeline once without retrying. Values below one throw an `ArgumentException` during registration. The other options are not validated by `WithRetry`; choose non-negative, finite delays and a sensible finite multiplier.

After each retry wait, the next delay is calculated as the current delay multiplied by `BackoffMultiplier`, capped at `MaxDelay`. The initial `Delay` is not clamped to `MaxDelay`. For example, five attempts with a one-second initial delay, multiplier two, and a three-second cap wait **1, 2, 3, and 3 seconds**. There is no wait after the final failure and no jitter.

### Exception filtering

Use `ShouldRetry` to restrict retries to failures that may succeed on a later attempt. This example retries only timeouts and I/O failures:

```csharp
using System.Reflection;
using MinimalKafka;
using MinimalKafka.Middleware.Resilience;

app.MapTopic("retry", (KafkaContext context) =>
	Task.FromException(new IOException($"Temporary failure for {context.Topic}.")))
	.WithDeadLetter()
	.WithRetry(options =>
	{
		options.MaxAttempts = 5;
		options.Delay = TimeSpan.FromMilliseconds(500);
		options.BackoffMultiplier = 2;
		options.MaxDelay = TimeSpan.FromSeconds(5);
		options.ShouldRetry = exception =>
		{
			var failure = exception is TargetInvocationException invocation
				? invocation.InnerException ?? invocation
				: exception;

			return failure is TimeoutException or IOException;
		};
	});
```

This registration is an alternative to the quick-start mapping, not an additional mapping for the same topic.

The handler adapter invokes delegates through reflection. A handler that throws synchronously can surface as a `TargetInvocationException`; a returned task that faults is awaited and normally exposes its underlying exception directly. The retry predicate receives the exception as caught, so account for wrapping when matching types. Binding and deserialization failures inside the handler adapter are also downstream failures.

Returning `false` rethrows immediately without waiting or attempting again. With an outer `WithDeadLetter()`, that failure is eligible for dead-letter routing immediately. The predicate should itself be safe to execute; if it throws, its exception propagates instead.

## Cancellation

Retries check `KafkaContext.CancellationToken.IsCancellationRequested` when a downstream attempt throws. If cancellation is requested, the exception is rethrown without evaluating `ShouldRetry` or scheduling another attempt. Retry waits use that same token, so shutdown can interrupt a delay.

Dead-letter middleware does not catch a failure when the context token is canceled. An `OperationCanceledException` thrown while that token is **not** canceled is treated like any other exception by the default retry policy and can be dead-lettered.

These policies do not impose a handler timeout or forcibly stop an operation that ignores cancellation. Pass the supplied cancellation token into asynchronous I/O performed by the handler.

## Offsets and failure outcomes

The consumer disables automatic offset commit and automatic offset storage. It explicitly commits the consumed result after the complete consumer pipeline returns successfully.

| Outcome | Behavior |
| --- | --- |
| Handler succeeds initially or on a retry | Pipeline returns; the background service attempts to commit the offset. |
| Attempts are exhausted or `ShouldRetry` returns `false`, with no DLQ policy | Exception escapes; that message does not reach the explicit commit call. |
| DLQ publication succeeds | The original processing failure is handled; if the rest of the enclosing pipeline also succeeds, the service attempts to commit. |
| DLQ publication fails | The publication exception propagates; that message does not reach the explicit commit call. |
| Cancellation interrupts processing or a retry delay | The exception propagates; the failed invocation does not reach the commit call. |

An unhandled non-cancellation failure is logged as critical and rethrown by the background service, which closes the consumer. Host configuration determines the effect on the application; there is no internal loop that restarts a failed consumer service. After a restart or rebalance, uncommitted messages may be delivered again.

Successful processing, DLQ publication, and offset commits are not one atomic transaction. A commit failure or crash can cause duplicate handler side effects or duplicate DLQ messages. Make handlers and recovery workflows idempotent.

## Scope and throughput

Retry invokes the same downstream delegate using the same `KafkaContext`, middleware instances, and per-message dependency-injection scope. It does not create a fresh scope or reset changes made to the context or scoped services between attempts. Retried processing must tolerate partial work from previous attempts.

The background service consumes and processes one message at a time. Handler execution and retry waits delay the next `Consume` call for all topics and partitions handled by that service, not just the failing record's partition. Budget the complete processing and backoff duration against the Kafka consumer's `max.poll.interval.ms`. Retries are in-memory waits, not durable scheduled messages; attempt counts reset on a new delivery.

## Global middleware and claim checks

Global consumer middleware runs before per-topic middleware. Failures before it invokes the topic pipeline, or after that pipeline returns, cannot be caught by the inner topic-level retry and dead-letter policies.

In particular, [claim-check middleware](claims-check-pattern.md) retrieves a stored value before entering the topic pipeline. A missing claim or retrieval error is therefore not retried or dead-lettered by these per-topic policies. Claim deletion also happens outside them.

When `DeleteAfterConsume` is enabled, an outer claim-check middleware can delete a stored payload after an inner dead-letter middleware handles a failure and returns successfully. This matters because dead-letter routing copies the claim header. Preserve external payloads for the required recovery window; see [DLQ claim-check considerations](dead-letter-queues.md#claim-check-interactions).

## Kafka client resilience versus handler resilience

Kafka connection and delivery behavior is configured through the `KafkaConsumerOptions.Common`, `Consumer`, and `Producer` dictionaries, or their corresponding configuration sections. Client-specific settings override `Common` settings. These are Kafka client settings, not additional MinimalKafka handler policies.

`WithRetry` is consumer middleware: it does not wrap arbitrary application calls to `IKafkaProducer.ProduceAsync`. With the recommended policy order, it also does not retry a failed DLQ publication because that publication happens in the outer dead-letter middleware. Errors in `Consume` and `Commit` occur outside both policies.

Use Kafka client configuration for broker delivery behavior and application-specific middleware or handlers for additional policies. MinimalKafka does not currently supply a circuit breaker, an overall retry deadline, or an automatic DLQ replay service.

## Observability

Retry middleware logs:

- **Warning** for a failed attempt that will be retried, including topic, attempt count, and delay.
- **Warning** when cancellation is observed while handling a failed attempt.
- **Error** for a non-retryable exception or exhausted attempts.

Dead-letter routing logs the processing exception and destination at **Error** level before publishing; this is not proof that publication succeeded. Escaped failures are also logged by the consumer host. Multiple log records can describe different stages of the same failed message.

Monitor retry frequency, processing latency, consumer lag, DLQ publication failures, and recovery backlog. Log messages are diagnostics; they are not a durable retry history.
