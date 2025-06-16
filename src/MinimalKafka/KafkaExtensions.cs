using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;
using System.Text.Json;

namespace MinimalKafka;

public interface IAddKafkaBuilder : IKafkaConventionBuilder
{
    IServiceCollection Services { get; }
}


internal class AddKafkaBuilder(IServiceCollection services, ICollection<Action<IKafkaBuilder>> conventions) 
    : KafkaConventionBuilder(conventions, []), IAddKafkaBuilder
{
    public IServiceCollection Services { get; } = services;
}


public static class KafkaExtensions
{
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services, Action<IAddKafkaBuilder> config)
    {
        var conventions = new List<Action<IKafkaBuilder>>();
        var configBuilder = new AddKafkaBuilder(services, conventions);

        configBuilder.WithClientId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithGroupId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithAutoCommit(false);
        configBuilder.WithKeyDeserializer(typeof(JsonTextSerializer<>));
        configBuilder.WithValueDeserializer(typeof(JsonTextSerializer<>));
        configBuilder.WithTopicFormatter(topic => topic);

        config(configBuilder);

        services.TryAddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        services.AddTransient(typeof(JsonTextSerializer<>));

        services.AddSingleton<IKafkaBuilder>(s =>
        {
            var b = new KafkaBuilder(s);
            conventions.ForEach(x => x(b));
            return b;
        });
        services.AddHostedService<KafkaService>();

        services.AddSingleton(typeof(IProducer<,>), typeof(KafkaProducerFactory<,>));

        return services;
    }

    public static IKafkaConventionBuilder MapTopic(this IApplicationBuilder builder, string topic, Delegate handler)
    {
        var tb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return tb.MapTopic(topic, handler);
    }
    public static IKafkaConventionBuilder MapTopic(this IKafkaBuilder builder, string topic, Delegate handler)
    {
        return builder
            .GetOrAddTopicDataSource()
            .AddTopicDelegate(topic, handler)
            .WithMetaData([.. builder.MetaData]);
    }

    private static IKafkaDataSource GetOrAddTopicDataSource(this IKafkaBuilder builder)
    {
        builder.DataSource ??= new KafkaDataSource(builder.ServiceProvider);
        return builder.DataSource;
    }
}
