using Confluent.Kafka;
using Examples;
using MinimalKafka;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage.RocksDB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka(config =>
 {
     config
           .WithConfiguration(builder.Configuration.GetSection("Kafka"))
           .WithGroupId(Guid.NewGuid().ToString())
           .WithOffsetReset(AutoOffsetReset.Earliest)
           .WithJsonSerializers()
           .UseRocksDB(options =>
           {
               options.Path = "c:\\SourceCode\\rocksdb";
           });

 });

var app = builder.Build();

app.MapTopic("test", (KafkaContext context) => {
    
    Console.WriteLine("Test topic received message: " + context.Value);

    throw new Exception("Test exception");

});

await app.RunAsync();
