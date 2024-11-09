using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;
public static class StreamExtensions
{
    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IApplicationBuilder builder, string topic)
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();

        return sb.MapStream<TKey, TValue>(topic);
    }

    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IKafkaBuilder builder, string topic)
    {
        var sb = new StreamBuilder<TKey, TValue>(builder, topic);
        return sb;
    }

    public static IIntoBuilder<TKey, (V1?, V2?)> OnKey<TKey, V1, V2>(this IJoinBuilder<TKey, V1, TKey, V2> builder)
        => builder.On((k, _) => k, (k, _) => k);

    public static IKafkaConventionBuilder Into<TKey, V1, V2>(this IIntoBuilder<TKey, (V1?, V2?)> builder, string topic) 
        => builder.Into(async (c, k, v) =>
        {
            await c.ProduceAsync(topic, k, v);
        });
}

