namespace MinimalKafka.Stream;

/// <summary>
/// Configures stream branching behavior.
/// </summary>
public sealed class StreamBranchBuilder<TKey, TValue>
{
    private readonly List<BranchDefinition> _branches = [];

    internal IReadOnlyList<BranchDefinition> Branches => _branches;

    internal Func<StreamContext, TKey, TValue, Task>? DefaultHandler { get; private set; }

    /// <summary>Adds a branch with a custom handler.</summary>
    public void Branch(Func<TKey, TValue, bool> predicate, Func<StreamContext, TKey, TValue, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        _branches.Add(new BranchDefinition(predicate, handler));
    }

    /// <summary>Adds a branch that republishes to another topic.</summary>
    public RoutedBranch Branch(Func<TKey, TValue, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new RoutedBranch(this, predicate);
    }

    /// <summary>Sets the default handler when no branch matches.</summary>
    public void DefaultBranch(Func<StreamContext, TKey, TValue, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        DefaultHandler = handler;
    }

    /// <summary>Sets the default branch to republish to another topic.</summary>
    public void DefaultBranch(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        DefaultHandler = (context, key, value) => context.ProduceAsync(topic, key, value);
    }

    internal sealed record BranchDefinition(
        Func<TKey, TValue, bool> Predicate,
        Func<StreamContext, TKey, TValue, Task> Handler);

    /// <summary>Represents a branch that routes to a topic.</summary>
    public sealed class RoutedBranch(StreamBranchBuilder<TKey, TValue> builder, Func<TKey, TValue, bool> predicate)
    {
        /// <summary>Republishes matching records to the specified topic.</summary>
        public void To(string topic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            builder.Branch(predicate, (context, key, value) => context.ProduceAsync(topic, key, value));
        }
    }
}
