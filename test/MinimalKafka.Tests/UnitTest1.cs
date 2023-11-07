using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Tests;

public class Tests
{
    [Test]
    public void Test1()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMinimalKafka();

        var app = builder.Build();

        app.MapTopic("test", (string key, string value) =>
        {
            Debug.WriteLine(key + ":" + value);
        });

        app.Run();
    }
}
