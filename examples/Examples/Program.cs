using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config.WithBootstrapServers("nas:9092")
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithKeyDeserializer(typeof(JsonTextSerializer<>))
           .WithValueDeserializer(typeof(JsonTextSerializer<>))
           .WithKeySerializer(typeof(JsonTextSerializer<>))
           .WithValueSerializer(typeof(JsonTextSerializer<>));
 });

var store = new InMemoryStore<Guid, Tuple<string?, string?>>();

builder.Services.AddHostedService(x => store);

var app = builder.Build();

app.MapStream<Guid, string>("left")
    .Join<Guid, string>("right").On(store, (k1, v1) => k1, (k2, v2) => k2)
    .Into(async (c, k, v) =>
    {
        if(v.Item1 is null || v.Item2 is null)
        {
            return;
        }

        Console.WriteLine(v.Item1 + v.Item2);

        var result = await c.ProduceAsync("result", k, v.Item1 + v.Item2);

        Console.WriteLine(result.Message);
    });

app.MapTopic("topic.name", async (KafkaContext context, string key, string value) =>
{
    Console.WriteLine($"{key} - {value}");

    var result =  await context.ProduceAsync("test2", key, value);

    Console.WriteLine($"Produced {result.Status}");

});

await app.RunAsync();