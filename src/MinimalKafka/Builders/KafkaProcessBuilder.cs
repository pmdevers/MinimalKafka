using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Middlewares;

namespace MinimalKafka.Builders;

/// <summary>
/// Represents the configuration and processing pipeline for a Kafka consumer, including metadata, middleware, and a
/// delegate for handling Kafka-related operations.
/// </summary>
public sealed class KafkaProcessBuilder(IServiceProvider serviceProvider)
{
    /// <summary>
    /// The Unique ConsumerKey.
    /// </summary>
    public KafkaConsumerKey? Key { get; private set; }

    /// <summary>
    /// Gets the <see cref="KafkaConsumerBuilder"/> instance used to configure and build the Kafka consumer.
    /// </summary>
    public IKafkaConsumerBuilder ConsumerBuilder { get; private set; } = new KafkaConsumerBuilder(serviceProvider);

    /// <summary>
    /// Gets the metadata associated with the current instance.
    /// </summary>
    public IReadOnlyList<object> Metadata { get; private set; } = [];

    /// <summary>
    /// Gets the collection of middleware components to be executed in the Kafka processing pipeline.
    /// </summary>
    /// <remarks>Middleware components are used to customize or extend the behavior of the Kafka processing
    /// pipeline. Each middleware function is responsible for invoking the next middleware in the pipeline.</remarks>
    public IList<Func<IServiceProvider, KafkaMiddlewareDelegate>> Middlewares { get; private set; } = [];

    /// <summary>
    /// Gets the delegate responsible for handling Kafka-related operations.
    /// </summary>
    public KafkaDelegate Delegate { get; private set; } = (_) => Task.CompletedTask;

    /// <summary>
    /// Creates a new instance of the <see cref="KafkaProcessBuilder"/> class using the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies required by the <see cref="KafkaProcessBuilder"/> instance.
    /// Cannot be <see langword="null"/>.</param>
    /// <returns>A new instance of the <see cref="KafkaProcessBuilder"/> class.</returns>

    public static KafkaProcessBuilder Create(IServiceProvider serviceProvider)
        => new(serviceProvider);

    /// <summary>
    /// Creates a new instance of <see cref="KafkaProcessBuilder"/> with the specified key.
    /// </summary>

    public KafkaProcessBuilder WithKey(KafkaConsumerKey key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="KafkaProcessBuilder"/> with the specified metadata.
    /// </summary>

    public KafkaProcessBuilder WithMetadata(List<object> metaData)
    {
        Metadata = metaData;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="KafkaProcessBuilder"/> with the specified middleware components.
    /// </summary>

    public KafkaProcessBuilder WithMiddleware(IList<Func<IServiceProvider, KafkaMiddlewareDelegate>> middlewares)
    {
        Middlewares = middlewares;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="KafkaProcessBuilder"/> with the specified delegate.
    /// </summary>

    public KafkaProcessBuilder WithDelegate(KafkaDelegate @delegate)
    {
        Delegate = @delegate;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="KafkaProcessBuilder"/> with the specified consumer builder.
    /// </summary>
    /// <param name="consumerBuilder">The consumer builder to be used by the Kafka process.</param>
    /// <returns>The current <see cref="KafkaProcessBuilder"/> instance.</returns>
    public KafkaProcessBuilder WithConsumerBuilder(IKafkaConsumerBuilder consumerBuilder)
    {
        ConsumerBuilder = consumerBuilder;
        return this;
    }



    /// <summary>
    /// Builds and returns an <see cref="IKafkaProcess"/> instance configured with the specified key, metadata, and
    /// middleware pipeline.
    /// </summary>

    internal IKafkaProcess Build()
    {
        ArgumentNullException.ThrowIfNull(Key);

        var consumerBuilder = ConsumerBuilder
            .WithKey(Key)
            .WithMetadata(Metadata);

        Middlewares.Add((_) => async (ctx, next) =>
        {
            await Delegate(ctx);
            await next(ctx);
        });

        return ActivatorUtilities.CreateInstance<KafkaProcess>(serviceProvider, consumerBuilder, Middlewares.AsReadOnly());
    }
}

