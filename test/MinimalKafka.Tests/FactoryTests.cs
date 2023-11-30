using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using MinimalKafka.Attributes;
using MinimalKafka.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka.Tests;
internal class FactoryTests
{
    [Test]
    public void FactoryTest()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddTransient<Dummy>();

        var del = (KafkaContext context) => { };

        //var result = TopicDelegateFactory.InferMetaData(del.Method, new()
        //{
        //    ServiceProvider = serviceCollection.BuildServiceProvider(),
        //});

        var result = TopicDelegateFactory.Create(Handle, new()
        {
            ServiceProvider = serviceCollection.BuildServiceProvider(),
        });

        var message = new Confluent.Kafka.Message<string, string>()
        {
            Key = "hello",
            Value = "hello",
        };

        var context = new KafkaContext<string, string>(message, serviceCollection.BuildServiceProvider());

        result.TopicDelegate.Invoke(context);
    }

    public Task Handle(KafkaContext context, Dummy dummy, [FromKey] string key)
    {
        return Task.CompletedTask;
    }
}

public class Dummy
{
    public int Id { get; set; } = 1;
}
