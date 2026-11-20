using System.Collections.Concurrent;

namespace MinimalKafka.Runtime;

/// <summary>
/// Represents the registration details for a topic consumer, including the topic name, handler delegate, optional
/// message format, and middleware pipeline.
/// </summary>
/// <remarks>Use this type to configure how a topic is consumed, including optional middleware components resolved
/// from an <see cref="IServiceProvider"/>.</remarks>
/// <param name="topic">The topic name to associate with the consumer registration.</param>
/// <param name="handler">The delegate that handles messages published to the topic.</param>
public sealed class TopicRegistration(string topic, ConsumerDelegate handler)
{
    /// <summary>
    /// Gets the topic associated with the current instance.
    /// </summary>
    public string Topic { get; } = topic;
    /// <summary>
    /// Gets or sets the delegate that handles consumed messages.
    /// </summary>
    public ConsumerDelegate Handler { get; set; } = handler;
    /// <summary>
    /// Gets or sets the format string.
    /// </summary>
    public string? Format { get; set; }
    /// <summary>
    /// Gets the list of middleware components associated with the current instance.
    /// </summary>
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
}