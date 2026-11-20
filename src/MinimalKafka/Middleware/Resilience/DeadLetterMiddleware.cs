using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MinimalKafka.Middleware.Resilience;

/// <summary>
/// Publishes a message that failed downstream processing to a dead letter topic and lets the consumer move on.
/// If publishing to the dead letter topic fails, the exception propagates and the offset is not committed.
/// </summary>
internal sealed class DeadLetterMiddleware(string deadLetterTopic, ILogger<DeadLetterMiddleware> logger) : IConsumerMiddleware
{
    public const string OriginalTopicHeader = "x-original-topic";
    public const string OriginalPartitionHeader = "x-original-partition";
    public const string OriginalOffsetHeader = "x-original-offset";
    public const string ExceptionTypeHeader = "x-exception-type";
    public const string ExceptionMessageHeader = "x-exception-message";

    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (!context.CancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Message at {Topic}[{Partition}]@{Offset} failed; sending to {DeadLetterTopic}.",
                context.Topic, context.Partition, context.Offset, deadLetterTopic);

            var headers = new KafkaHeaders(context.Headers);
            headers.Set(OriginalTopicHeader, context.Topic);
            headers.Set(OriginalPartitionHeader, context.Partition.ToString());
            headers.Set(OriginalOffsetHeader, context.Offset.ToString());
            headers.Set(ExceptionTypeHeader, exception.GetType().FullName ?? exception.GetType().Name);
            headers.Set(ExceptionMessageHeader, Truncate(exception.Message, 1024));

            await context.Producer.ProduceAsync(
                deadLetterTopic,
                context.Value ?? [],
                context.Key,
                headers,
                cancellationToken: context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
