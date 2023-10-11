using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;
public class KafkaOptions
{
    public string BoostrapServers { get;set; } = "localhost:9092";
 
    public string ConsumerGroupId { get; set; } = Guid.NewGuid().ToString();
}

public static class ServiceCollectionExtentions
{
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services)
    {
        services.AddSingleton(new TopicConsumerBuilder(new KafkaOptions()));
        return services;
    }
}
