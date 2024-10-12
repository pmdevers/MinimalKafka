using Microsoft.AspNetCore.Builder;
using System.Diagnostics;

namespace MinimalKafka.Stream;
public static class KafkaStreamExtensions
{
    public static KafkaStreamBuilder MapStream<TKey, TLeft, TRight>(this IApplicationBuilder builder,
        Func<KafkaContext, Tuple<TKey, TLeft, TRight>, Task> handler
        )
        where TKey : IEquatable<TKey>
    {
        var process = new KafkaStream<TKey, TLeft, TRight>(
            TimeSpan.FromSeconds(5), handler
        );
        return new KafkaStreamBuilder(builder, process);
    }
}

public class KafkaStreamBuilder
{
    private readonly IApplicationBuilder _builder;
    private readonly IKafkaStream _stream;

    public KafkaStreamBuilder(IApplicationBuilder builder, IKafkaStream stream)
    {
        _builder = builder;
        _stream = stream;
    }

    public KafkaStreamBuilder WithLeft(string left) 
    {
        _builder.MapTopic(left, _stream.GetLeft());
        return this;
    }

    public KafkaStreamBuilder WithRight(string right)
    {
        _builder.MapTopic(right, _stream.GetRight());
        return this;
    }
}

