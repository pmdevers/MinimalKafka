namespace MinimalKafka.Stream;

/// <summary>
/// Defines a builder interface for configuring conditional branching logic for Kafka message processing.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
public interface IBranchBuilder<out TKey, out TValue>
{
    /// <summary>
    /// Adds a branch with a predicate and a handler method to be executed when the predicate matches.
    /// </summary>
    /// <param name="predicate">A function that determines whether the branch should be taken based on the key and value.</param>
    /// <param name="method">A handler function to execute if the predicate returns true.</param>
    /// <returns>The same <see cref="IBranchBuilder{TKey, TValue}"/> instance for chaining.</returns>
    IBranchBuilder<TKey, TValue> Branch(Func<TKey, TValue, bool> predicate, Func<KafkaContext, TKey, TValue, Task> method);

    /// <summary>
    /// Sets the default branch handler to be executed if no predicates match.
    /// </summary>
    /// <param name="method">A handler function to execute if no branch predicates match.</param>
    /// <returns>The same <see cref="IBranchBuilder{TKey, TValue}"/> instance for chaining.</returns>
    IBranchBuilder<TKey, TValue> DefaultBranch(Func<KafkaContext, TKey, TValue, Task> method);
}
