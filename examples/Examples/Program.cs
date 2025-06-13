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
           .WithAutoCommit(false)
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithJsonSerializers()
           .WithInMemoryStore()
           .Use(async (ctx, next) =>
           {
               await next();

               Console.WriteLine("From Middleware");
           });
 });

var app = builder.Build();

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