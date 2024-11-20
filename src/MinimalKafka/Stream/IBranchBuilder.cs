using MinimalKafka.Builders;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;

public interface IBranchBuilder<out TKey, out TValue>
{
    IBranchBuilder<TKey, TValue> Branch(Func<TKey, TValue, bool> predicate, Func<KafkaContext, TKey, TValue, Task> method);
    IBranchBuilder<TKey, TValue> DefaultBranch(Func<KafkaContext, TKey, TValue, Task> method);
}

public static class IntoBuilderExtensions
{
    public static IKafkaConventionBuilder SplitInto<TKey, TValue>(this IIntoBuilder<TKey, TValue> builder,
        Action<IBranchBuilder<TKey, TValue>> branches)
    {
        BranchBuilder<TKey, TValue> branch = new();
        branches?.Invoke(branch);
        Func<KafkaContext, TKey, TValue, Task> func = branch.Build();
        return builder.Into(func);
    }

    public static IKafkaConventionBuilder SplitInto<TValue>(this IIntoBuilder<TValue> builder,
        Action<IBranchBuilder<Guid, TValue>> branches)
    {
        BranchBuilder<Guid, TValue> branch = new();
        branches?.Invoke(branch); 
        Func<KafkaContext, Guid, TValue, Task> func = branch.Build();
        return builder.Into((c, v) => func(c, Guid.NewGuid(), v));
    }
}