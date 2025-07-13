using MinimalKafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config.WithBootstrapServers("nas:9092");
           //.WithConfiguration(builder.Configuration.GetSection("Kafka"));
           //.WithOffsetReset(AutoOffsetReset.Earliest)
           //.WithPartitionAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))
           //.WithJsonSerializers()
           //.UseRocksDB();

 });

var app = builder.Build();

app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
{
    Console.WriteLine($"Received: {key} - {value}");

    Console.WriteLine("##################");
    Console.WriteLine("my-topic");
    Console.WriteLine("##################");
}).WithGroupId("test");


app.MapTopic("my-topic", ([FromKey] string key, [FromValue] string value) =>
{
    Console.WriteLine($"Received: {key} - {value}");

    Console.WriteLine("##################");
    Console.WriteLine("my-topic");
    Console.WriteLine("##################");
}).WithGroupId("test2");

//app.MapStream<Guid, LeftObject>("left")
//    .Join<int, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
//    .Into((c, v) =>
//    {
//        var (left, right) = v;

//        Console.WriteLine("##################");
//        Console.WriteLine("LEFT Join Right");
//        Console.WriteLine("##################");

//        return Task.CompletedTask;
//    }).WithGroupId("group1");

//app.MapStream<Guid, LeftObject>("left")
//    .Into(async (c, k, v) =>
//    {
//        v = v with { RightObjectId = 2 };

//        Console.WriteLine("##################");
//        Console.WriteLine("LEFT INTO UPDATE");
//        Console.WriteLine("##################");

//        await c.ProduceAsync("left-update", k, v);
//    }).WithGroupId("group2");


//app.MapStream<int, RightObject>("right")
//    .Join<Guid, LeftObject>("left").On((k, v) => k, (k, v) => v.RightObjectId)
//    .Into((c, k, v) =>
//    {
//        var (left, right) = v;

//        Console.WriteLine("##################");
//        Console.WriteLine("RIGHT JOIN LEFT");
//        Console.WriteLine("##################");

//        return Task.CompletedTask;
//    })
//    .WithGroupId("group3");


await app.RunAsync();