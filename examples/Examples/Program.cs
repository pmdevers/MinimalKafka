using Confluent.Kafka;
using Examples;
using MinimalKafka;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
{
    config
        .WithBootstrapServers("localhost:19092")
        .WithOffsetReset(AutoOffsetReset.Earliest)
        .WithPartitionAssignedHandler((_, p) => {
            return p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning));
        })
        .WithJsonSerializers()
        .WithInMemoryStore();
});

var app = builder.Build();

app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
{
    Console.WriteLine($"Received: {key} - {value}");
});

app.MapStream<Guid, LeftObject>("left")
    .Join<int, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
    .Into((c, v) =>
    {
        var (left, right) = v;

        return Task.CompletedTask;
    }).WithGroupId("group1");

app.MapStream<Guid, LeftObject>("left")
    .Into(async (c, k, v) =>
    {
        v = v with { RightObjectId = 2 };
        await c.ProduceAsync("left-update", k, v);
    }).WithGroupId("group2");


app.MapStream<int, RightObject>("right")
    .Join<Guid, LeftObject>("left").On((k, v) => k, (k, v) => v.RightObjectId)
    .Into((c, k, v) =>
    {
        var (left, right) = v;

        return Task.CompletedTask;
    })
    .WithGroupId("group3");


app.MapStream<int, RightObject>("right")
    .Join<Guid, LeftObject>("left").On((k, v) => k, (k, v) => v.RightObjectId)
    .Into((c, k, v) =>
    {
       throw new InvalidOperationException("this will not commit");
    })
    .WithGroupId("group4");



await app.RunAsync();