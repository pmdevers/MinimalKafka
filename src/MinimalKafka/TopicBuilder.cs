using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware;
using MinimalKafka.Runtime;

namespace MinimalKafka;

public sealed class TopicBuilder
{
    private readonly TopicRegistration _registration;

    internal TopicBuilder(TopicRegistration registration) => _registration = registration;

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




