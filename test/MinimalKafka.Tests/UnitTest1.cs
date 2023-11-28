using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Tests;

public class Tests
{
    [Test]
    public async Task Test1()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMinimalKafka();

        var app = builder.Build();

        app.MapTopic("order.snapshot", (IServiceProvider provider, string key, string value) =>
        {
            Debug.WriteLine(key + ":" + value);
        });

        app.MapTopic("order.events", (string key, string value) =>
        {
            Debug.WriteLine(key + ":" + value);
        });

        //var topics = app.Services.GetRequiredService<TopicConsumer>();

        await app.RunAsync();


    }
}
