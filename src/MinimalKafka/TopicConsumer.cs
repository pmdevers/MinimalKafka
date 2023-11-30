using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;

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
                        object? value = GetPrameterValue(result, scope.ServiceProvider, item)
                            ?? throw new InvalidOperationException($"Could not bind parameter: '{item.Name}'");

                        invokeArguments.Add(value);
                    }

                    _topic.TopicHandler.DynamicInvoke(invokeArguments.ToArray());
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

    

    private static object? GetPrameterValue(ConsumeResult<string, string>? result, IServiceProvider provider, ParameterInfo item)
    {
        object? value = item.GetCustomAttribute<FromServicesAttribute>() != null ?
                                    provider.GetRequiredService(item.ParameterType) :
                                    provider.GetService(item.ParameterType);

        if (HasKeyAttribute(item))
        {
            value = result.Message.Key;
        }

        if (HasValueAttribute(item))
        {
            value = result.Message.Value;
        }

        if (HasHeaderAttribute(item))
        {
            var header = item.GetCustomAttribute<FromHeaderAttribute>();
            value = Encoding.UTF8.GetString(result.Headers.Single(x => x.Key == header.Name).GetValueBytes());
        }

        return value;
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

    public static bool HasKeyAttribute(ParameterInfo parameter) =>
        parameter.Name.Equals("Key", StringComparison.CurrentCultureIgnoreCase) ||
        parameter.GetCustomAttribute<FromKeyAttribute>() is not null;

    public static bool HasValueAttribute(ParameterInfo parameter) =>
        parameter.Name.Equals("Key", StringComparison.CurrentCultureIgnoreCase) ||
        parameter.GetCustomAttribute<FromValueAttribute>() is not null;

    public static bool HasHeaderAttribute(ParameterInfo parameter) =>
        parameter.GetCustomAttribute<FromHeaderAttribute>() is not null;
}


public class ParameterResolver
{
    public ParameterResolver(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    
}

public interface IParameterAttribute
{
    public string Name { get; set; }
}
