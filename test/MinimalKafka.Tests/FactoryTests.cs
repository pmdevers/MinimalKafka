using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;
using MinimalKafka.Factory;

namespace MinimalKafka.Tests;

internal class FactoryTests
{
    [Test]
    public void FactoryTest()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddTransient<Dummy>();

        var result = TopicDelegateFactory.Create(Handle, new()
        {
            ServiceProvider = serviceCollection.BuildServiceProvider(),
        });

        var consumeResult = new Confluent.Kafka.ConsumeResult<string, string>()
        {
            Message = new()
            {
                Key = "hello",
                Value = "hello from Value",
            },
        };

        var context = KafkaContext.Create(consumeResult, serviceCollection.BuildServiceProvider());

        result.TopicDelegate.Invoke(context);
    }

    public Task Handle(KafkaContext context, [FromServices] Dummy dummy, string key, [FromValue] string value)
    {
        return Task.CompletedTask;

    }
}

public class Dummy
{
    public int Id { get; set; } = 1;
}
