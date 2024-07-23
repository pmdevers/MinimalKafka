using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config.WithBootstrapServers("nas.home.lab:9092")
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithKeySerializer(Deserializers.Utf8)
           .WithValueSerializer(Deserializers.Utf8);
 });

var app = builder.Build();

app.MapTopic("topic.name", (string key, string value) =>
{

    Console.WriteLine($"{key} - {value}");
    return Task.CompletedTask;

});

await app.RunAsync();
