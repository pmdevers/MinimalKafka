using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;

internal class TopicConsumer
{
    private readonly Topic _topic;
    private readonly IServiceProvider _serviceProvider;

    public TopicConsumer(Topic topic, IServiceProvider serviceProvider)
    {
        _topic = topic;
        _serviceProvider = serviceProvider;
    }

    public void StartAsync(CancellationToken cancellation)
    {
        var consumer = BuildConsumer();

        consumer.Subscribe(_topic.TopicName);

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellation);
                

                if (result is not null)
                {
                    using var scope = _serviceProvider.CreateScope();

                    var arguments = _topic.TopicHandler.GetMethodInfo().GetParameters();
                    var invokeArguments = new List<object>();
                    foreach (var item in arguments)
                    {
                        var service = scope.ServiceProvider.GetService(item.ParameterType);
                        if (service is not null)
                        {
                            invokeArguments.Add(service);
                        }

                        if (item.Name.Equals("Key", StringComparison.CurrentCultureIgnoreCase))
                        {
                            invokeArguments.Add(result.Message.Key);
                        }

                        if (item.Name.Equals("Value", StringComparison.CurrentCultureIgnoreCase))
                        {
                            invokeArguments.Add(result.Message.Value);
                        }
                    }

                    _topic.TopicHandler.DynamicInvoke(invokeArguments.ToArray());
                   Debug.Write(result.Message.Value);
                }
            }
        } 
        catch (OperationCanceledException)
        {

        } 
        finally
        {
            consumer.Close();
        }
    }

    private IConsumer<string, string> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "nas.home.lab:9092",
            GroupId = "Bla", // Use type name for unique group ID
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        return new ConsumerBuilder<string, string>(config)
            .Build();
    }
}
