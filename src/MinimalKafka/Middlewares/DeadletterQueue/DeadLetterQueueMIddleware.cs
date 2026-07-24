using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalKafka.Helpers;

namespace MinimalKafka.Middlewares.DeadletterQueue;

/// <summary>
/// Middleware for handling messages that cannot be processed and need to be sent to a dead letter queue.
/// </summary>
public class DeadLetterQueueMiddleware(IDeadLetterResolver resolver, IOptions<DeadLetterQueueOptions> options, ILogger<DeadLetterQueueMiddleware> logger) : IKafkaMiddleware
{
    private readonly DeadLetterQueueOptions _options = options.Value;
    private readonly ILogger<DeadLetterQueueMiddleware> _logger = logger;

    /// <inheritdoc />
    public async Task InvokeAsync(KafkaContext context, KafkaDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var resolutionKey = resolver.MarkPending(context);

            var headers = new Dictionary<string, string>(context.Headers)
            {
                ["dlq.source.topic"] = context.TopicName,
                ["dlq.source.partition"] = context.Partition.ToString(),
                ["dlq.source.offset"] = context.Offset.ToString(),
                ["dlq.source.group"] = context.GroupId,
                ["dlq.resolution.key"] = resolutionKey,
                ["dlq.exception.type"] = ex.InnerException?.GetType().FullName ?? ex.GetType().Name,
                ["dlq.exception.message"] = ex.InnerException?.Message ?? ex.Message
            };

            context.Produce(new KafkaMessage(_options.Topic, context.Key.ToArray(), context.Value.ToArray(), headers));

            _logger.DeadLetterQueued(_options.Topic, context.TopicName, context.Partition, context.Offset, resolutionKey);

            if (_options.ResolveAutomatically)
            {
                _logger.DeadLetterAutoResolved(context.TopicName, context.Partition, context.Offset);
                resolver.Resolve(context.TopicName, context.Partition, context.Offset);
            }
        }
    }
}
