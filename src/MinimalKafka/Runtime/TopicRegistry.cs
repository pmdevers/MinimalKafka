using System.Collections.Concurrent;

namespace MinimalKafka.Runtime;

internal sealed class TopicRegistration(string topic, ConsumerDelegate handler)
{
    public string Topic { get; } = topic;
    public ConsumerDelegate Handler { get; set; } = handler;
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
}