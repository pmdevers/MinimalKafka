using Confluent.Kafka;
using MinimalKafka;
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

var store = new InMemoryStore<Guid, Tuple<string?, string?>>();

builder.Services.AddHostedService(x => store);

var app = builder.Build();

app.MapStream<Guid, string>("left")
    .Join<Guid, string>("right").OnKey()
    .Into(async (c, k, v) =>
    {
        if(v.Item1 is null || v.Item2 is null)
        {
            return;
        }

        Console.WriteLine(v.Item1 + v.Item2);

        var result = await c.ProduceAsync("result", k, v.Item1 + v.Item2);

        Console.WriteLine(result.Message);
    }).WithClientId("MapStream");

app.MapTopic("topic.name", async (KafkaContext context, string key, string value) =>
{
    Console.WriteLine($"{key} - {value}");

    var result =  await context.ProduceAsync("test2", key, value);

    Console.WriteLine($"Produced {result.Status}");

}).WithClientId("MapTopic");

await app.RunAsync();