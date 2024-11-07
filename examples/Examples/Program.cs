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

var store = new InMemoryStore<Guid, Tuple<string?, string?>>();

builder.Services.AddHostedService(x => store);

var app = builder.Build();


app.MapStream();


await app.RunAsync();