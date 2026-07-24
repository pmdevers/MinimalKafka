using System.Collections.Concurrent;

namespace MinimalKafka.Middlewares.DeadletterQueue;

/// <summary>
/// Resolves dead-letter queue items before the source pipeline commits offsets.
/// </summary>
public interface IDeadLetterResolver
{
    /// <summary>
    /// Marks the source message as pending resolution.
    /// </summary>
    string MarkPending(KafkaContext context);

    /// <summary>
    /// Returns true when the source message is pending resolution.
    /// </summary>
    bool HasPending(KafkaContext context);

    /// <summary>
    /// Waits until the source message is resolved.
    /// </summary>
    Task WaitForResolutionAsync(KafkaContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the pending DLQ item for a source message identity.
    /// </summary>
    bool Resolve(string topic, int partition, long offset);
}

internal sealed class InMemoryDeadLetterResolver : IDeadLetterResolver
{
    private readonly ConcurrentDictionary<ResolutionKey, TaskCompletionSource> _pending = new();

    public string MarkPending(KafkaContext context)
    {
        var key = ResolutionKey.Create(context.TopicName, context.Partition, context.Offset);
        _pending.TryAdd(key, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return key;
    }

    public bool HasPending(KafkaContext context)
        => _pending.ContainsKey(ResolutionKey.Create(context.TopicName, context.Partition, context.Offset));

    public async Task WaitForResolutionAsync(KafkaContext context, CancellationToken cancellationToken)
    {
        if (!_pending.TryGetValue(ResolutionKey.Create(context.TopicName, context.Partition, context.Offset), out var completion))
        {
            return;
        }

        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        try
        {
            await completion.Task;
        }
        finally
        {
            _pending.TryRemove(ResolutionKey.Create(context.TopicName, context.Partition, context.Offset), out _);
        }
    }

    public bool Resolve(string topic, int partition, long offset)
    {
        var resolutionKey = ResolutionKey.Create(topic, partition, offset);
        if (!_pending.TryGetValue(resolutionKey, out var completion))
        {
            return false;
        }

        return completion.TrySetResult();
    }
}


/// <summary>
/// 
/// </summary>
/// <param name="Topic"></param>
/// <param name="Partition"></param>
/// <param name="Offset"></param>
public record struct ResolutionKey(string Topic, int Partition, long Offset)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="partition"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static ResolutionKey Create(string topic, int partition, long offset)
        => new(topic, partition, offset);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override readonly string ToString()
        => $"{Topic}:{Partition}:{Offset}";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static implicit operator string(ResolutionKey value) => value.ToString();
}