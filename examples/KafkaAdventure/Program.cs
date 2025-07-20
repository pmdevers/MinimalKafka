using Confluent.Kafka;
using KafkaAdventure.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using MinimalKafka;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddMinimalKafka(x =>
{
    x.WithBootstrapServers("localhost:19092")
     .WithTopicFormatter((topic) => $"{topic}-{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower()}")
     .WithGroupId(AppDomain.CurrentDomain.FriendlyName + "-test")
     .WithClientId(AppDomain.CurrentDomain.FriendlyName + "-test")
     .WithOffsetReset(AutoOffsetReset.Earliest)
     .WithJsonSerializers(x =>
     {
         x.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
         x.Converters.Add(new JsonStringEnumConverter());
     })
     .UseRocksDB();
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

app.UsePathBase("/kafka-adventure");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHealthChecks("/startup");
app.UseHealthChecks("/liveness");
app.UseHealthChecks("/ready");



app.MapLocationsApi();

app.MapLocations();

app.MapInput();
app.MapCommand();
app.MapGo();
app.MapHelp();
app.MapLook();

app.MapOutput();


Console.WriteLine("Starting Up");

await app.RunAsync();

