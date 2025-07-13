using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;

/// <summary>
/// Class with extension methods for <see cref="IBranchBuilder{TKey, TValue}"/>.
/// </summary>
public static class IIntoBuilderExtensions
{
    /// <summary>
    /// Configures the specified builder to produce messages to the given Kafka topic.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="V1">The type of the first value in the message payload.</typeparam>
    /// <typeparam name="V2">The type of the second value in the message payload.</typeparam>
    /// <param name="builder">The builder used to configure the Kafka message production.</param>
    /// <param name="topic">The name of the Kafka topic to which messages will be produced. Cannot be null or empty.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> instance configured to produce messages to the specified topic.</returns>
    public static IKafkaConventionBuilder Into<TKey, V1, V2>(this IIntoBuilder<TKey, (V1?, V2?)> builder, string topic)
        => builder.Into((c, k, v) =>
        {
            c.Produce(topic, k, v);
            return Task.CompletedTask;
        });

    /// <summary>
    /// Splits the current Kafka stream into multiple branches based on custom logic.
    /// </summary>
    /// <remarks>This method enables conditional routing of Kafka messages to different destinations based on
    /// user-defined logic. Each branch can define a predicate to filter messages and an associated action to handle
    /// them.</remarks>
    /// <typeparam name="TKey">The type of the key in the Kafka stream.</typeparam>
    /// <typeparam name="TValue">The type of the value in the Kafka stream.</typeparam>
    /// <param name="builder">The <see cref="IIntoBuilder{TKey, TValue}"/> used to define the target Kafka topics or destinations.</param>
    /// <param name="branches">A delegate that configures the branching logic by defining one or more branches using an <see
    /// cref="IBranchBuilder{TKey, TValue}"/>. Each branch specifies a condition and a corresponding action for
    /// processing messages.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> that allows further configuration of the Kafka stream.</returns>
    public static IKafkaConventionBuilder SplitInto<TKey, TValue>(this IIntoBuilder<TKey, TValue> builder,
        Action<IBranchBuilder<TKey, TValue>> branches)
    {
        BranchBuilder<TKey, TValue> branch = new();
        branches?.Invoke(branch);
        Func<KafkaContext, TKey, TValue, Task> func = branch.Build();
        return builder.Into(func);
    }

    /// <summary>
    /// Splits the processing pipeline into multiple branches, allowing custom logic to be applied to each branch.
    /// </summary>
    /// <remarks>This method allows you to define multiple branches in the processing pipeline, each with its
    /// own logic. A new unique identifier is generated for each value processed in the branches.</remarks>
    /// <typeparam name="TValue">The type of the value being processed in the pipeline.</typeparam>
    /// <param name="builder">The builder used to configure the pipeline.</param>
    /// <param name="branches">A delegate that defines the branching logic. The delegate receives an <see cref="IBranchBuilder{TKey, TValue}"/>
    /// to configure individual branches.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> that represents the configured pipeline after branching.</returns>
    public static IKafkaConventionBuilder SplitInto<TValue>(this IIntoBuilder<TValue> builder,
        Action<IBranchBuilder<Guid, TValue>> branches)
    {
        BranchBuilder<Guid, TValue> branch = new();
        branches?.Invoke(branch);
        Func<KafkaContext, Guid, TValue, Task> func = branch.Build();
        return builder.Into((c, v) => func(c, Guid.NewGuid(), v));
    }
}
