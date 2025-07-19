using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;

/// <summary>
/// Provides extension methods for <see cref="IBranchBuilder{TKey, TValue}"/> to facilitate branching logic.
/// </summary>
/// <remarks>These methods enable the creation of branches and default branches for processing keyed
/// values.</remarks>
public static class IBranchBuilderExtensions
{
    /// <summary>
    /// Creates a new branch in the current branching context based on the specified selection criteria.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used in the branching context.</typeparam>
    /// <typeparam name="TValue">The type of the value associated with the key in the branching context.</typeparam>
    /// <param name="builder">The branching context to which the new branch will be added. Cannot be null.</param>
    /// <param name="selector">A function that determines whether a key-value pair should be included in the new branch.  The function must
    /// return <see langword="true"/> for inclusion; otherwise, <see langword="false"/>.</param>
    /// <returns>A <see cref="ToBranchBuilder{TKey, TValue}"/> instance representing the newly created branch.</returns>
    public static IToBranchBuilder<TKey, TValue> Branch<TKey, TValue>(
        this IBranchBuilder<TKey, TValue> builder,
        Func<TKey, TValue, bool> selector)
    {
        return new ToBranchBuilder<TKey, TValue>(builder, selector);
    }

    /// <summary>
    /// Configures the builder to use a default branch for processing unmatched records.
    /// </summary>
    /// <remarks>The default branch is used to handle records that do not match any other branch conditions.
    /// Records are produced to the specified topic asynchronously.</remarks>
    /// <typeparam name="TKey">The type of the key in the records being processed.</typeparam>
    /// <typeparam name="TValue">The type of the value in the records being processed.</typeparam>
    /// <param name="builder">The branch builder to configure.</param>
    /// <param name="topicName">The name of the topic to which unmatched records will be produced. Cannot be null or empty.</param>
    /// <returns>An <see cref="IBranchBuilder{TKey, TValue}"/> instance configured with the default branch.</returns>
    public static IBranchBuilder<TKey, TValue> DefaultBranch<TKey, TValue>(
        this IBranchBuilder<TKey, TValue> builder,
        string topicName)
        => builder.DefaultBranch(async (c, k, v) => await c.ProduceAsync(topicName, k, v));
}
