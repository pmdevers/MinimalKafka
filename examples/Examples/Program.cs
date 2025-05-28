using Confluent.Kafka;
using Examples;
using MinimalKafka;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config
           .WithConfiguration(builder.Configuration.GetSection("Kafka"))
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithJsonSerializers()
           .WithInMemoryStore();
 });

var app = builder.Build();

app.MapStream1<Guid, LeftObject>("left")
    .Join<int, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
    .Into((c, v) =>
    {
        var (left, right) = v;

        return Task.CompletedTask;
    });

await app.RunAsync();



public class StreamPipelineBuilder<K1, V1, K2, V2>(IKafkaBuilder builder, string left, string right) 
{
    private readonly Func<KafkaContext, IStreamStore<K1, V1>> _getLeftStore = (KafkaContext context) 
        => context.RequestServices.GetRequiredService<IStreamStore<K1, V1>>();

    private readonly Func<KafkaContext, IStreamStore<K2, V2>> _getRightStore = (KafkaContext context) 
        => context.RequestServices.GetRequiredService<IStreamStore<K2, V2>>();

    private Func<V1, V2, bool> _on = (_, _ ) => true;
    private Func<KafkaContext, (V1?, V2?), Task> _into = (_, _) => Task.CompletedTask;

    public IKafkaBuilder Builder { get; } = builder;
    
    public StreamPipelineBuilder<K1, V1, K2, V2> On(Func<V1, V2, bool> on)
    {
        _on = on;
        return this;
    }

    public IKafkaConventionBuilder Into(Func<KafkaContext, (V1, V2), Task> into)
    {
        _into = into;

        var l = Builder.MapTopic(left, ExecuteLeftAsync);
        var r = Builder.MapTopic(right, ExecuteRightAsync);
        return l;
    }

    public async Task ExecuteLeftAsync(KafkaContext context, K1 key, V1 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var left = await leftStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        var right = await rightStore.FindAsync(x => _on(value, x))
                              .LastOrDefaultAsync();

        await _into(context, (left, right));
    }

    public async Task ExecuteRightAsync(KafkaContext context, K2 key, V2 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var right = await rightStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        var left = await leftStore.FindAsync(x => _on(x, value))
                              .LastOrDefaultAsync();

        await _into(context, (left, right));
    }
}

public class StreamPipelineBuilder<K1, V1>(IKafkaBuilder builder, string left)
{
    public StreamPipelineBuilder<K1, V1, K2, V2> Join<K2, V2>(string topic)
    {
        return new StreamPipelineBuilder<K1, V1, K2, V2>(builder, left, topic);
    }
}

public static class Extensions
{
    public static StreamPipelineBuilder<K1, V1> MapStream1<K1, V1>(this IApplicationBuilder builder, string topic)
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return sb.MapStream1<K1, V1>(topic);
    }

    public static StreamPipelineBuilder<K1, V1> MapStream1<K1, V1>(this IKafkaBuilder builder, string topic)
    {
        return new StreamPipelineBuilder<K1, V1>(builder, topic);
    }
}