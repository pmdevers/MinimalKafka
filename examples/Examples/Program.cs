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
           .WithValueDeserializer(typeof(JsonTextSerializer<>));
 });

var app = builder.Build();

app.MapStream<Guid, Left, Right>((c, v) => 
    { 
     Console.WriteLine($"{v.Item1} - {v.Item2.value}, {v.Item3.value}");
    return Task.CompletedTask;
})
    .WithLeft("left")
    .WithRight("right");

app.MapTopic("topic.name", async (KafkaContext context, string key, string value) =>
{
    Console.WriteLine($"{key} - {value}");

    var result =  await context.ProduceAsync("test2", key, value);

    Console.WriteLine($"Produced {result.Status}");

});

await app.RunAsync();

#pragma warning disable S3903 // Types should be defined in named namespaces
public record Left(Guid id, string value);

public record Right(Guid id, string value);

#pragma warning restore S3903 // Types should be defined in named namespaces