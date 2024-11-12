using Confluent.Kafka;
using Examples;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using MinimalKafka;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

app.MapStream<Guid, AggregateEvent<Guid>>("eventstream")
    .Aggregate<Guid, Aggregate<Guid>>("aggregate", builder =>
    {
        builder.AddEvent<ChangeName>((v, e) => v with { Name = e.Name });
        builder.AddEvent<ChangeSurName>((v, e) => v with { SurName = e.Surname });
    })
    .WithGroupId("Aggregate")
    .WithClientId("Aggregate");

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


app.MapStream<Guid, LeftObject>("left")
   .Into((c, k, v) =>
   {
       Console.WriteLine($"single Into - {k} - {v}");
       return Task.CompletedTask;
   })
   .WithGroupId($"single-{Guid.NewGuid()}")
   .WithClientId("single");


await app.RunAsync();
