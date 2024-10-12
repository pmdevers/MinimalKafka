using MinimalKafka.Stream;
using static System.Net.Mime.MediaTypeNames;

namespace MinimalKafka.Tests;

public class KafkaStreamBuilderTests
{
    public class ApiTest
    {
        public void Test()
        {
        }
    }
}



public class KafkaStream<TKey, TLeft, TRight>(
    TimeSpan expirationTime,
    Func<KafkaContext, Tuple<TKey, TLeft, TRight>, Task> handler,
    Func<TLeft, TKey>? _leftKeySelector = null,
    Func<TRight, TKey>? _rightKeySelector = null
    )
    where TKey : IEquatable<TKey>
{
    private readonly TimedConcurrentDictionary<TKey, TLeft> _left = new(expirationTime);
    private readonly TimedConcurrentDictionary<TKey, TRight> _right = new(expirationTime);

    public Task Left(KafkaContext context, TKey key, TLeft value)
    {
        var k = _leftKeySelector is null ? key : _leftKeySelector.Invoke(value);
        _left.AddOrUpdate(k, value);

        return Process(context, k);
    }

    public Task Right(KafkaContext context, TKey key, TRight value)
    {
        var k = _rightKeySelector is null ? key : _rightKeySelector.Invoke(value);
        _right.AddOrUpdate(k, value);

        return Process(context, k);
    }

    private async Task Process(KafkaContext context, TKey key)
    {
        if(_left.GetItems().TryGetValue(key, out var left) &&
           _right.GetItems().TryGetValue(key, out var right))
        {
            await handler(context, Tuple.Create(key, left, right));
        }
    }
}

