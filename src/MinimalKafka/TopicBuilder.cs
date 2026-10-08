using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware;
using MinimalKafka.Runtime;

namespace MinimalKafka;

/// <summary>
/// Builds per-topic consumer configuration, including message format and middleware pipeline.
/// </summary>
public sealed class TopicBuilder
{
    private readonly TopicRegistration _registration;

    internal TopicBuilder(TopicRegistration registration) => _registration = registration;

    /// <summary>Gets the Kafka topic name for this builder.</summary>
    public string Topic => _registration.Topic;

    /// <summary>Sets the message format used to deserialize values of this topic, see <see cref="Serialization.MessageFormats"/>.</summary>
    public TopicBuilder UseFormat(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        _registration.Format = format;
        return this;
    }

    /// <summary>Adds a middleware type resolved per message. Constructor arguments not in DI can be passed as <paramref name="arguments"/>.</summary>
    public TopicBuilder Use<TMiddleware>(params object[] arguments)
        where TMiddleware : class, IConsumerMiddleware
    {
        _registration.Middleware.Add(services =>
            ActivatorUtilities.CreateInstance<TMiddleware>(services, arguments));
        return this;
    }

    /// <summary>Adds an inline middleware delegate to the topic pipeline.</summary>
    /// <param name="middleware">
    /// A delegate that receives the current <see cref="KafkaContext"/> and the next <see cref="ConsumerDelegate"/> in the pipeline.
    /// </param>
    /// <returns>The current <see cref="TopicBuilder"/> instance.</returns>
    public TopicBuilder Use(Func<KafkaContext, ConsumerDelegate, Task> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _registration.Middleware.Add(_ => new DelegateMiddleware(middleware));
        return this;
    }

    private sealed class DelegateMiddleware(Func<KafkaContext, ConsumerDelegate, Task> middleware) : IConsumerMiddleware
    {
        public Task InvokeAsync(KafkaContext context, ConsumerDelegate next) => middleware(context, next);
    }
}




