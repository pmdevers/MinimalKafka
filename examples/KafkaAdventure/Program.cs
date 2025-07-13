using KafkaAdventure.Features.CommandProcessor;
using KafkaAdventure.Features.Input;
using KafkaAdventure.Features.Locations;
using KafkaAdventure.Features.Movement;
using KafkaAdventure.Features.PlayerLocation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using MinimalKafka;
using MinimalKafka.Serializers;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<LocationContext>();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddMinimalKafka(x =>
{
    x.WithConfiguration(builder.Configuration.GetSection("kafka"));
    x.WithTopicFormatter((topic) =>
    {
        return $"{topic}-{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower()}";
    });
    x.WithJsonSerializers();
    x.UseRocksDB();
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

app.MapInput();
app.MapProcessor();
app.MapMovement();
app.MapPlayerLocations();
app.MapLocations();


Console.WriteLine("Starting Up");

app.Run();

