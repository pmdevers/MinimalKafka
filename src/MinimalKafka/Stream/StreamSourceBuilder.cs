using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka.Stream;

/// <summary>
/// Fluent builder for a typed Kafka stream source.
/// </summary>
public sealed class StreamSourceBuilder<TKey, TValue>
{
    private readonly IApplicationBuilder _builder;
    private readonly TopicRegistry _registry;
    private readonly string _topicName;

    internal StreamSourceBuilder(IApplicationBuilder builder, TopicRegistry registry, string topicName)
    {
        _builder = builder;
        _registry = registry;
        _topicName = topicName;
    }

    /// <summary>The physical Kafka topic mapped by this stream.</summary>
    public string TopicName => _topicName;

    /// <summary>Creates a key-based join against another topic.</summary>
    public StreamJoinBuilder<TKey, TValue, TOtherKey, TOtherValue> Join<TOtherKey, TOtherValue>(string topic) =>
        new(_builder, _registry, _topicName, ResolveTopic(topic), innerJoin: false);

    /// <summary>Creates an inner key-based join against another topic.</summary>
    public StreamJoinBuilder<TKey, TValue, TOtherKey, TOtherValue> InnerJoin<TOtherKey, TOtherValue>(string topic) =>
        new(_builder, _registry, _topicName, ResolveTopic(topic), innerJoin: true);

    /// <summary>Registers a terminal processor for the stream.</summary>
    public void Into(Func<StreamContext, TKey, TValue, Task> processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        Register(async context =>
        {
            await StoreCurrentMessageAsync(context).ConfigureAwait(false);

            var key = await StreamSerialization.DeserializeAsync<TKey>(
                context.RequestServices,
                context.Key,
                _topicName,
                context.Headers,
                context.CancellationToken,
                context.Format).ConfigureAwait(false);

            var value = await StreamSerialization.DeserializeAsync<TValue>(
                context.RequestServices,
                context.Value,
                _topicName,
                context.Headers,
                context.CancellationToken,
                context.Format).ConfigureAwait(false);

            await processor(new StreamContext(context), key!, value!).ConfigureAwait(false);
        });
    }

    /// <summary>Registers branching behavior for the stream.</summary>
    public void SplitInto(Action<StreamBranchBuilder<TKey, TValue>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var branches = new StreamBranchBuilder<TKey, TValue>();
        configure(branches);

        Register(async context =>
        {
            await StoreCurrentMessageAsync(context).ConfigureAwait(false);

            var key = await StreamSerialization.DeserializeAsync<TKey>(
                context.RequestServices,
                context.Key,
                _topicName,
                context.Headers,
                context.CancellationToken,
                context.Format).ConfigureAwait(false);

            var value = await StreamSerialization.DeserializeAsync<TValue>(
                context.RequestServices,
                context.Value,
                _topicName,
                context.Headers,
                context.CancellationToken,
                context.Format).ConfigureAwait(false);

            var streamContext = new StreamContext(context);
            foreach (var branch in branches.Branches)
            {
                if (!branch.Predicate(key!, value!))
                {
                    continue;
                }

                await branch.Handler(streamContext, key!, value!).ConfigureAwait(false);
                return;
            }

            if (branches.DefaultHandler is not null)
            {
                await branches.DefaultHandler(streamContext, key!, value!).ConfigureAwait(false);
            }
        });
    }

    private void Register(Func<KafkaContext, Task> handler)
    {
        _registry.Add(_topicName, handler);
    }

    private string ResolveTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        return _builder.ApplicationServices.GetRequiredService<ITopicNamingConvention>().Apply(topic);
    }

    internal static async Task StoreCurrentMessageAsync(KafkaContext context)
    {
        if (context.Key is null || context.Value is null)
        {
            return;
        }

        var storeFactory = context.RequestServices.GetRequiredService<IKafkaStoreFactory>();
        var store = storeFactory.GetStore(context.Topic);
        await store.AddOrUpdate(context.Key, context.Value).ConfigureAwait(false);
    }
}
