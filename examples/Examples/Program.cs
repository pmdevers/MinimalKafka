using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config.WithBootstrapServers("nas.home.lab:9092")
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithKeyDeserializer(Deserializers.Utf8)
           .WithValueDeserializer(Deserializers.Utf8);
 });

var app = builder.Build();

app.MapTopic("topic.name", async (KafkaContext context, string key, string value) =>
{
    Console.WriteLine($"{key} - {value}");

    var result =  await context.ProduceAsync("test2", key, value);

    Console.WriteLine($"Produced {result.Status}");

});

await app.RunAsync();
