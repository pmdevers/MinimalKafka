using MinimalKafka;
using MinimalKafka.Stream;
using System.ComponentModel.Design;
using System.Runtime.InteropServices.Marshalling;

namespace Examples.Aggregates;

public static class AggregatesExtensions
{
    public static IAggregateBuilder<TKey, TEvent, TState> Aggregate<TKey, TEvent, TState>(
        this IIntoBuilder<TKey, (TEvent?, TState)> intoBuilder, 
        Action<IAggregateBuilder<TKey, TEvent, TState>> aggregateBuilder)
        where TState : IAggregate<TKey, TEvent, TState>
        where 
    {
        var builder = new AggregateBuilder<TKey, TEvent, TState>();
        aggregateBuilder.Invoke(builder);
        Func<KafkaContext, TKey, (TEvent, TState), Task> func = builder.Build();
        return builder.Into(func);
    }
}

public interface IAggregate<TKey, TState, TEvent>
    where TState : IAggregate<TKey, TState, TEvent>
    where TEvent : IEvent<TKey>
{
    TKey Key { get; }
    int Version { get; }
    abstract static TState Create(TKey key);

    abstract static void Configure(IAggregateBuilder<TKey, TEvent, TState> builder);
}

public interface IEvent<TKey>
{
    TKey Key { get; }
    int Version { get; }
}


/// <summary>
/// Static helper class for creating <see cref="Result{TResult}"/> objects representing operation outcomes.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a failed <see cref="Result{TResult}"/> with the specified state and error messages.
    /// </summary>
    /// <typeparam name="TResult">The type of the result state.</typeparam>
    /// <param name="value">The state value associated with the result.</param>
    /// <param name="errorMessage">One or more error messages describing the failure.</param>
    /// <returns>
    /// A <see cref="Result{TResult}"/> representing a failed operation, containing the specified state and error messages.
    /// </returns>
    public static Result<TResult> Failed<TResult>(TResult value, params string[] errorMessage)
        => new()
        {
            State = value,
            ErrorMessage = errorMessage,
            IsSuccess = false
        };
}

/// <summary>
/// Represents the outcome of an operation, including its result value, success status, and error messages (if any).
/// </summary>
/// <typeparam name="T">The type of the result state.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets or sets the state value associated with the result.
    /// </summary>
    public required T State { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets or sets the collection of error messages if the operation failed.
    /// Defaults to an empty array.
    /// </summary>
    public string[] ErrorMessage { get; init; } = [];

    /// <summary>
    /// Implicitly converts a state value of type <typeparamref name="T"/> to a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The state value to wrap as a successful result.</param>
    public static implicit operator Result<T>(T value)
        => new()
        {
            State = value,
            IsSuccess = true
        };
}

public interface IAggregateBuilder<TKey, TEvent, TState>
    where TState : IAggregate<TKey, TState, TEvent>
    where TEvent : IEvent<TKey>
{
    IAggregateBuilder<TKey, TEvent, TState> AddHandler(Func<TEvent, bool> when, Func<TState, TEvent, Task<Result<TState>>> then);
}

public interface IWhenBuilder<TKey, TEvent, TState>
{
    IThenBuilder<TKey, TEvent, TState> Then(Func<TState, TEvent, Result<TState>> handler);
}

public interface IThenBuilder<TKey, TEvent, TState>
{

}

public interface ITo

internal class AggregateBuilder<TKey, TEvent, TState>() : IAggregateBuilder<TKey, TEvent, TState>
    where TState : IAggregate<TKey, TState, TEvent>
    where TEvent: IEvent<TKey>
{
    private readonly ICollection<EventHandler<TKey, TEvent, TState>> _branches = [];

    private Func<TState, TEvent, Task<Result<TState>>> _default =
        (state, e) => Task.FromResult<Result<TState>>(state);

    public IAggregateBuilder<TKey, TEvent, TState> AddHandler(Func<TEvent, bool> when, Func<TState, TEvent, Task<Result<TState>>> then)
    {
        var eventhandler = new EventHandler<TKey, TEvent, TState>(when, then);
        _branches.Add(eventhandler);
        return this;
    }

    public Func<KafkaContext, TKey, (TEvent?, TState?), Task> Build()
    {
        return async (c, k, v) =>
        {
            var (@event, state) = v;

            if(@event is null)
            { 
                return; 
            }

            if(state is null)
            {
                state = TState.Create(k);
            }

            if(@event.Version != state.Version)
            {
                return;
            }

            var branch = _branches.FirstOrDefault(x => x.When(@event));
            var result = await (branch?.Then(state, @event) ?? _default(state, @event));

            if (result.IsSuccess)
            {
                await c.ProduceAsync("", k, result.State);
            }
        };
    }
}

public record EventHandler<TKey, TEvent, TState>(
    Func<TEvent, bool> When,
    Func<TState, TEvent, Task<Result<TState>>> Then
    );

