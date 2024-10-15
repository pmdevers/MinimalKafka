using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;
public static class KafkaStreamExtensions
{
    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IApplicationBuilder builder, string topic)
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();

        return sb.MapStream<TKey, TValue>(topic);
    }

    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IKafkaBuilder builder, string topic)
    {
        var sb = new KafkaStreamBuilder<TKey, TValue>(builder, topic);
        return sb;
    }
}

