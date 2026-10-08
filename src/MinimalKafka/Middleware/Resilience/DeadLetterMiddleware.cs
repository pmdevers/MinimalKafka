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
                context.Topic, context.ConsumeResult.Partition.Value, context.ConsumeResult.Offset.Value, deadLetterTopic);

            var headers = new Headers();
            foreach (var header in context.Headers)
            {
                headers.Add(header.Key, header.GetValueBytes());
            }
            headers.Add(OriginalTopicHeader, Encoding.UTF8.GetBytes(context.Topic));
            headers.Add(OriginalPartitionHeader, Encoding.UTF8.GetBytes(context.ConsumeResult.Partition.Value.ToString()));
            headers.Add(OriginalOffsetHeader, Encoding.UTF8.GetBytes(context.ConsumeResult.Offset.Value.ToString()));
            headers.Add(ExceptionTypeHeader, Encoding.UTF8.GetBytes(exception.GetType().FullName ?? exception.GetType().Name));
            headers.Add(ExceptionMessageHeader, Encoding.UTF8.GetBytes(Truncate(exception.Message, 1024)));

            await context.Producer.ProduceAsync(
                deadLetterTopic,
                context.Value ?? [],
                context.Key,
                headers,
                context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
