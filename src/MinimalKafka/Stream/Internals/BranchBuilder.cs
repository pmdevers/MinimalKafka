namespace MinimalKafka.Stream.Internals;

internal class BranchBuilder<TKey, TValue> : IBranchBuilder<TKey, TValue>
{
    private readonly Dictionary<string, Func<KafkaContext, TKey, TValue, Task>> _branches = [];
    private Func<KafkaContext, TKey, TValue, Task> _default;
    private readonly Func<TValue, string> _branchSelector;

    public BranchBuilder(Func<TValue, string> branchSelector)
    {
        _branchSelector = branchSelector;
        _default = (_, _, value) => throw new UnhandledBranchException(_branchSelector(value));
    }
    public IBranchBuilder<TKey, TValue> Branch(string name, Func<KafkaContext, TKey, TValue, Task> method)
    {
        _branches.Add(name, method);
        return this;
    }

    public IBranchBuilder<TKey, TValue> DefaultBranch(Func<KafkaContext, TKey, TValue, Task> method)
    {
        _default = method;
        return this;
    }
    public Func<KafkaContext, TKey, TValue, Task> Build()
    {
        return (c, k, v) =>
        {
            string name = _branchSelector(v);
            return _branches.TryGetValue(name, out Func<KafkaContext, TKey, TValue, Task>? branch) 
                ? branch(c, k, v)
                : _default(c, k, v);
        };
    }
}

public class UnhandledBranchException(string branchName) : Exception(branchName)
{
}