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
           .WithConfiguration(builder.Configuration.GetSection("Kafka"))
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithJsonSerializers()
           .WithInMemoryStore();
 });

var app = builder.Build();

app.MapStream<Guid, Command>("commands")
    .Split(branches =>
    {
        branches.Branch((_, v) => v.Name == "cmd1", (_, _, _) => Task.CompletedTask);
        branches.Branch((_, v) => v.Name == "cmd2", (_, _, _) => Task.CompletedTask);
        branches.DefaultBranch((context, key, value) => Task.CompletedTask);
    });

app.MapStream<Guid, LeftObject>("left")
    .Join<Guid, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
    .Split(branches =>
    {
        branches.Branch((_, _) => true, (_, _, _) => Task.CompletedTask);
    });

app.MapStream<Guid, LeftObject>("left")
    .Join<int, RightObject>("right").On((l, r) => l.RightObjectId == r.Id)
    .Into(async (c, value) =>
    {
        var (left, right) = value;
        var new_value = new ResultObject(left.Id, right);
        Console.WriteLine($"multi into - {left.Id} - {new_value}");
        await c.ProduceAsync("result", left.Id, new ResultObject(left.Id, right));
    })
    .WithGroupId($"multi-{Guid.NewGuid()}")
    .WithClientId("multi");

app.MapStream<Guid,LeftObject>("left")
    .Join<Guid, RightObject>("right")
    .OnKey()
    .Into("string");


app.MapStream<Guid, LeftObject>("left")
   .Into((c, k, v) =>
   {
       Console.WriteLine($"single Into - {k} - {v}");
       return Task.CompletedTask;
   })
   .WithGroupId($"single-{Guid.NewGuid()}")
   .WithClientId("single");

await app.RunAsync();
