namespace MinimalKafka.Stream.Internals;

internal record Branch<TKey, TValue>(
    Func<TKey, TValue, bool> Predicate,
    Func<KafkaContext, TKey, TValue, Task> BranchAction
);

internal class BranchBuilder<TKey, TValue> : IBranchBuilder<TKey, TValue>
{
    private readonly ICollection<Branch<TKey, TValue>> _branches = [];
    private Func<KafkaContext, TKey, TValue, Task> _default = 
        (_,_,_) => throw new UnhandledBranchException();

    public IBranchBuilder<TKey, TValue> Branch(Func<TKey, TValue, bool> predicate, Func<KafkaContext, TKey, TValue, Task> method)
    {
        _branches.Add(new Branch<TKey, TValue>(predicate, method));
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
            var branch = _branches.FirstOrDefault(x => x.Predicate(k, v));
            return branch?.BranchAction(c, k, v) ?? _default(c, k, v);
        };
    }
}

public class UnhandledBranchException() : Exception()
{
}