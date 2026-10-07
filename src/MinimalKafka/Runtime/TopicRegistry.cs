using System.Collections.Concurrent;

namespace MinimalKafka.Runtime;

internal sealed class TopicRegistration(string topic, ConsumerDelegate handler)
{
    public string Topic { get; } = topic;
    public ConsumerDelegate Handler { get; } = handler;
    public string? Format { get; set; }
    public List<Func<IServiceProvider, Middleware.IConsumerMiddleware>> Middleware { get; } = [];
}

internal sealed class TopicRegistry
{
    private readonly ConcurrentDictionary<string, TopicRegistration> _topics = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TopicRegistration> Topics => _topics;

    public TopicRegistration Add(string topic, Delegate handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);
        var registration = new TopicRegistration(topic, HandlerAdapter.Create(handler));
        if (!_topics.TryAdd(topic, registration))
        {
            throw new InvalidOperationException($"A consumer is already mapped for topic '{topic}'.");
        }
        return registration;
    }
}