using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Metadata;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;
using System.Text.Json;
using TopicMetadata = MinimalKafka.Metadata.TopicMetadata;

namespace MinimalKafka;

public interface IAddKafkaBuilder : IKafkaConventionBuilder
{
    IAddKafkaBuilder WithStreamStore(Type streamStoreType);

    IAddKafkaBuilder WithDefaultTopicOptions();

    IAddKafkaBuilder WithTopicOptions<TMessage>(Func<Type, string>? namingConvention, TimeSpan? retentionPeriod);
}

public class AddKafkaBuilder(IServiceCollection services, ICollection<Action<IKafkaBuilder>> conventions)
    : KafkaConventionBuilder(conventions, []), IAddKafkaBuilder
{
    private static readonly Func<Type, string> DefaultNamingConvention = type => type
        .FQN()
        .Replace('<', '-')
        .Replace('<', '-')
        .ToLowerInvariant();

    private readonly ICollection<Action<IKafkaBuilder>> _conventions = conventions;

    public IServiceCollection Services { get; } = services;

    public IAddKafkaBuilder WithStreamStore(Type streamStoreType)
    {
        if (!Array.Exists(streamStoreType.GetInterfaces(),
                x => x.IsGenericType &&
                     x.GetGenericTypeDefinition() == typeof(IStreamStore<,>)
            ))
        {
            throw new InvalidOperationException($"Type: '{streamStoreType}' does not implement IStreamStore<,>");
        }

        Services.AddSingleton(typeof(IStreamStore<,>), streamStoreType);

        return this;
    }

    public IAddKafkaBuilder WithDefaultTopicOptions()
    {
        _conventions.Add(builder => builder.MetaData.Add(new TopicMetadata(
            DefaultNamingConvention,
            null)));

        return this;
    }

    public IAddKafkaBuilder WithTopicOptions<TMessage>(Func<Type, string>? namingConvention, TimeSpan? retentionPeriod)
    {
        _conventions.Add(builder => builder.MetaData.Add(new TopicMetadata<TMessage>(
            namingConvention ?? DefaultNamingConvention,
            retentionPeriod)));

        return this;
    }

    public IAddKafkaBuilder WithTopicOptions<TMessage>(string topicName, TimeSpan? retentionPeriod)
    {
        _conventions.Add(builder => builder.MetaData.Add(new TopicMetadata<TMessage>(
            _ => topicName,
            retentionPeriod)));

        return this;
    }
}

public static class KafkaExtensions
{
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services, Action<IAddKafkaBuilder> config)
    {
        var conventions = new List<Action<IKafkaBuilder>>();
        var configBuilder = new AddKafkaBuilder(services, conventions);

        configBuilder.WithKeyDeserializer(typeof(JsonTextSerializer<>));
        configBuilder.WithValueDeserializer(typeof(JsonTextSerializer<>));
        configBuilder.WithDefaultTopicOptions();

        config(configBuilder);

        services.TryAddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        services.AddTransient(typeof(JsonTextSerializer<>));

        services.AddSingleton<IAdminClient>(provider
            => new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = provider.GetRequiredService<IKafkaBuilder>().MetaData
                        .OfType<IBootstrapServersMetadata>().First().BootstrapServers
                })
                .Build());

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

    public static TBuilder WithMetaData<TBuilder>(this TBuilder builder, params object[] items)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b =>
        {
            foreach (var item in items)
            {
                b.MetaData.Add(item);
            }
        });

        return builder;
    }

    public static TBuilder WithSingle<TBuilder>(this TBuilder builder, object metadata)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.RemoveMetaData(metadata);
        builder.WithMetaData(metadata);
        return builder;
    }

    public static TBuilder RemoveMetaData<TBuilder>(this TBuilder builder, object item)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b =>
        {
            b.MetaData.RemoveAll(x => x.GetType() == item.GetType());
        });

        return builder;
    }

    private static IKafkaDataSource GetOrAddTopicDataSource(this IKafkaBuilder builder)
    {
        builder.DataSource ??= new KafkaDataSource(builder.ServiceProvider);
        return builder.DataSource;
    }


    public static Task<DeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(this KafkaContext context, string topic,
        TKey key, TValue value)
        => Produce(context, topic, new Message<TKey, TValue>() {Key = key, Value = value});

    public static async Task<DeliveryResult<TKey, TValue>> Produce<TKey, TValue>(this KafkaContext context,
        string topic, Message<TKey, TValue> message)
    {
        var producer = context.RequestServices.GetRequiredService<IProducer<TKey, TValue>>();
        return await producer.ProduceAsync(topic, message);
    }
}