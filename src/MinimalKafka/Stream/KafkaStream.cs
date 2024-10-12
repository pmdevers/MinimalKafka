using System.Collections.Concurrent;

namespace MinimalKafka.Stream;
public class KafkaStream<TKey, TLeft, TRight>(
    TimeSpan expirationTime,
    Func<KafkaContext, Tuple<TKey, TLeft, TRight>, Task> handler,
    Func<TLeft, TKey>? _leftKeySelector = null,
    Func<TRight, TKey>? _rightKeySelector = null
    ) : IKafkaStream
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, Tuple<TLeft?, TRight?, DateTime>> _state = [];

    public void CleanUp()
    {
        var now = TimeProvider.System.GetUtcNow();
        
        // Remove items where the timestamp is older than the expiration time
        foreach (var key in _state.Keys.ToList())
        {
            if (now - _state[key].Item3 > expirationTime)
            {
                _state.TryRemove(key, out _);
            }
        }
    }

    public Delegate GetLeft() => 
        (KafkaContext context, TKey key, TLeft value) =>
        {
            var k = _leftKeySelector is null ? key : _leftKeySelector.Invoke(value);
            var result = _state.AddOrUpdate(k, 
                (k) => Tuple.Create<TLeft?, TRight?, DateTime>(value, default, context.Timestamp),
                (k, v) => Tuple.Create<TLeft?, TRight?, DateTime>(value, v.Item2, context.Timestamp)
            );

            return Process(context, k, result);
        };
    public Delegate GetRight() =>
        (KafkaContext context, TKey key, TRight value) =>
    {
        var k = _rightKeySelector is null ? key : _rightKeySelector.Invoke(value);
        var result = _state.AddOrUpdate(k,
            (k) => Tuple.Create<TLeft?, TRight?, DateTime>(default, value, context.Timestamp),
            (k, v) => Tuple.Create<TLeft?, TRight?, DateTime>(v.Item1, value, context.Timestamp)
        );

        return Process(context, k, result);
    };

    private async Task Process(KafkaContext context, TKey key, Tuple<TLeft?, TRight?, DateTime> result)
    {
        if (result.Item1 is not null && result.Item2 is not null)
        {
            await handler(context, Tuple.Create(key, result.Item1, result.Item2));
        }
    }
}

public interface IKafkaStream
{
    void CleanUp();
    Delegate GetLeft();
    Delegate GetRight();
}