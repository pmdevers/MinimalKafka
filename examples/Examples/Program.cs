using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Attributes;
using MinimalKafka.Serialization;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
{
    config
        .WithConfiguration(builder.Configuration.GetSection("Kafka"))
        .WithAutoOffsetReset(AutoOffsetReset.Earliest)
        .WithPartitionsAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))

        .WithJsonSerializer(x =>
        {
            x.Converters.Add(new JsonStringEnumConverter());
        });
});

var app = builder.Build();


app.MapTopic("my-topic", async (KafkaContext context, [FromKey] int key, [FromValue] string value) =>
{
    await context.Producer.ProduceAsync("other-topic", key, value);
});

//app.MapJoinExample();
//app.MapAggregate<Test, Guid, TestCommands>("tests");

//app.MapBranchExample();


//app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
//{
//    Console.WriteLine($"Received: {key} - {value}");

//    Console.WriteLine("##################");
//    Console.WriteLine("my-topic");
//    Console.WriteLine("##################");
//});


//app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
//{
//    Console.WriteLine($"Received: {key} - {value}");

//    Console.WriteLine("##################");
//    Console.WriteLine("my-topic");
//    Console.WriteLine("##################");
//});

//app.MapStream<Guid, LeftObject>("left")
//    .Join<int, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
//    .Into((c, v) =>
//    {
//        var (left, right) = v;

//        Console.WriteLine("##################");
//        Console.WriteLine("LEFT Join Right");
//        Console.WriteLine("##################");

//        return Task.CompletedTask;
//    });

//app.MapStream<Guid, LeftObject>("left")
//    .Into(async (c, k, v) =>
//    {
//        v = v with { RightObjectId = 2 };

//        Console.WriteLine("##################");
//        Console.WriteLine("LEFT INTO UPDATE");
//        Console.WriteLine("##################");

//        await c.ProduceAsync("left-update", k, v);
//    });


//app.MapStream<int, RightObject>("right")
//    .Join<Guid, LeftObject>("left").On((k, v) => k, (k, v) => v.RightObjectId)
//    .Into((c, k, v) =>
//    {
//        var (left, right) = v;

//        Console.WriteLine("##################");
//        Console.WriteLine("RIGHT JOIN LEFT");
//        Console.WriteLine("##################");

//        return Task.CompletedTask;
//    });


await app.RunAsync();