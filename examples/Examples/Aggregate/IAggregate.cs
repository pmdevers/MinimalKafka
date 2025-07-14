namespace Examples.Aggregate;

public interface IAggregate<TKey, TState, TCommand>
{
    TKey Id { get; }
    int Version { get; }

    abstract static Result<TState> Apply(TState state, TCommand command);
    abstract static TState Create(TCommand command);
}
