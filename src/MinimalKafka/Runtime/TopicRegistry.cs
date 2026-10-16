using System.Collections.Concurrent;

namespace MinimalKafka.Runtime;

internal sealed class TopicRegistration(string topic, ConsumerDelegate handler)
{
    public string Topic { get; } = topic;
    public ConsumerDelegate Handler { get; set; } = handler;
    public string? Format { get; set; }
    public string? DeadLetterTopic { get; set; }
    public List<Func<IServiceProvider, Middleware.IConsumerMiddleware>> Middleware { get; } = [];
}

/// <summary>
/// Represents a registry for topics.
/// </summary>
public interface ITopicRegistry
{
    /// <summary>
    /// Gets the list of registered topics.
    /// </summary>
    /// <returns></returns>
    string[] GetTopics();

    /// <summary>
    /// Gets configured dead-letter destinations keyed by source topic.
    /// </summary>
    /// <returns>A mapping of source topic to dead-letter topic.</returns>
    IReadOnlyDictionary<string, string> GetDeadLetterTopics();
}

internal sealed class TopicRegistry : ITopicRegistry
{
    private readonly ConcurrentDictionary<string, TopicRegistration> _topics = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TopicRegistration> Topics => _topics;

    public TopicRegistration Add(string topic, Delegate handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);
        var newHandler = HandlerAdapter.Create(handler);

        if (!_topics.TryAdd(topic, new TopicRegistration(topic, newHandler)))
        {
            var existingRegistration = _topics[topic];
            var existingHandler = existingRegistration.Handler;

            // Wrap both delegates to execute simultaneously
            async Task combinedHandler(KafkaContext context)
            {
                var task1 = existingHandler(context);
                var task2 = newHandler(context);
                await Task.WhenAll(task1, task2).ConfigureAwait(false);
            }

            existingRegistration.Handler = combinedHandler;
        }

        return _topics[topic];
    }

    public string[] GetTopics()
        => [.. _topics.Keys];

    public IReadOnlyDictionary<string, string> GetDeadLetterTopics()
    {
        return _topics
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value.DeadLetterTopic))
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value.DeadLetterTopic!,
                StringComparer.Ordinal);
    }
}
