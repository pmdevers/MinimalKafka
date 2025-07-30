using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Stream;
using Qowaiv.DomainModel.Kafka.Domain;
using Qowaiv.DomainModel.Kafka.Features;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
{
    config
          .WithConfiguration(builder.Configuration.GetSection("Kafka"))
          .WithBootstrapServers("localhost:19092")
          .WithGroupId(AppDomain.CurrentDomain.FriendlyName)
          .WithClientId(AppDomain.CurrentDomain.FriendlyName)
          //.WithTransactionalId(AppDomain.CurrentDomain.FriendlyName)
          .WithOffsetReset(AutoOffsetReset.Earliest)
          .WithPartitionAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))
          .WithJsonSerializers(x =>
          {
              x.Converters.Add(new JsonStringEnumConverter());
          })
          .UseRocksDB(x =>
          {
              x.DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RocksDB");
          });

});

builder.Services.AddSingleton(typeof(IAggregateStore<,>), typeof(KafkaAggregateStore<,>));

var app = builder.Build();

app.MapCreate();

app.MapStream<TestId, KafkaEvent<TestId>>("TestAggregate")
    .Into(async (c, k, v) => {
        var store = (KafkaAggregateStore<TestId, TestAggregate>)c.RequestServices.GetRequiredService<IAggregateStore<TestId, TestAggregate>>();
        await store.SaveEvent(v);
     });

app.Run();
