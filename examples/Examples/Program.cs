using Confluent.Kafka;
using Examples;
using Examples.Aggregate;
using MinimalKafka;
using MinimalKafka.Aggregates;
using MinimalKafka.Stream;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config
           .WithConfiguration(builder.Configuration.GetSection("Kafka"))
           .WithBootstrapServers("nas:9092")
           .WithGroupId(AppDomain.CurrentDomain.FriendlyName)
           .WithClientId(AppDomain.CurrentDomain.FriendlyName)
           .WithTransactionalId(AppDomain.CurrentDomain.FriendlyName)
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithPartitionAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))
           .WithJsonSerializers()
           .UseRocksDB(x =>
           {
               x.DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RocksDB");
           });

 });

var app = builder.Build();


app.MapAggregate<Test, Guid, TestCommands>("tests");


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