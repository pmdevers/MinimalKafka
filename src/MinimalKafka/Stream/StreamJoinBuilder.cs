using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka.Stream;

/// <summary>
/// Fluent builder for joining two stream sources.
/// </summary>
public sealed class StreamJoinBuilder<TKey, TValue, TOtherKey, TOtherValue>
{
    private readonly TopicRegistry _registry;
    private readonly string _leftTopicName;
    private readonly string _rightTopicName;
    private readonly bool _innerJoin;

    internal StreamJoinBuilder(
        IApplicationBuilder builder,
        TopicRegistry registry,
        string leftTopicName,
        string rightTopicName,
        bool innerJoin)
    {
        _ = builder;
        _registry = registry;
        _leftTopicName = leftTopicName;
        _rightTopicName = rightTopicName;
        _innerJoin = innerJoin;
    }

    /// <summary>Uses the Kafka message key as the join condition.</summary>
    public KeyJoinedStreamBuilder OnKey() => new(_registry, _leftTopicName, _rightTopicName, _innerJoin);

    /// <summary>
    /// Terminal builder for a key-based join.
    /// </summary>
    public sealed class KeyJoinedStreamBuilder
    {
        private readonly TopicRegistry _registry;
        private readonly string _leftTopicName;
        private readonly string _rightTopicName;
        private readonly bool _innerJoin;

        internal KeyJoinedStreamBuilder(
            TopicRegistry registry,
            string leftTopicName,
            string rightTopicName,
            bool innerJoin)
        {
            _registry = registry;
            _leftTopicName = leftTopicName;
            _rightTopicName = rightTopicName;
            _innerJoin = innerJoin;
        }

        /// <summary>Registers a join processor that receives the left key and both joined values.</summary>
        public void Into(Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            ArgumentNullException.ThrowIfNull(processor);
            Register(async (streamContext, key, values) => await processor(streamContext, key, values).ConfigureAwait(false));
        }

        /// <summary>Registers a join processor that receives only the joined values.</summary>
        public void Into(Func<StreamContext, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            ArgumentNullException.ThrowIfNull(processor);
            Register(async (streamContext, _, values) => await processor(streamContext, values).ConfigureAwait(false));
        }

        private void Register(Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            _registry.Add(_leftTopicName, (Func<KafkaContext, Task>)(async context =>
            {
                await StreamSourceBuilder<TKey, TValue>.StoreCurrentMessageAsync(context).ConfigureAwait(false);
                await InvokeProcessorAsync(
                    context,
                    currentTopic: _leftTopicName,
                    otherTopic: _rightTopicName,
                    useLeftCurrentValue: true,
                    processor).ConfigureAwait(false);
            }));

            _registry.Add(_rightTopicName, (Func<KafkaContext, Task>)(async context =>
            {
                await StreamSourceBuilder<TKey, TValue>.StoreCurrentMessageAsync(context).ConfigureAwait(false);
                await InvokeProcessorAsync(
                    context,
                    currentTopic: _rightTopicName,
                    otherTopic: _leftTopicName,
                    useLeftCurrentValue: false,
                    processor).ConfigureAwait(false);
            }));
        }

        private async Task InvokeProcessorAsync(
            KafkaContext context,
            string currentTopic,
            string otherTopic,
            bool useLeftCurrentValue,
            Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            if (!useLeftCurrentValue)
            {
                _ = await StreamSerialization.DeserializeAsync<TOtherKey>(
                    context.RequestServices,
                    context.Key,
                    currentTopic,
                    context.Headers,
                    context.CancellationToken,
                    ResolveFormat(currentTopic)).ConfigureAwait(false);
            }

            var storeFactory = context.RequestServices.GetRequiredService<IKafkaStoreFactory>();
            var otherStore = storeFactory.GetStore(otherTopic);
            var otherValueBytes = context.Key is null
                ? null
                : await otherStore.FindByKeyAsync(context.Key).ConfigureAwait(false);

            var leftValueBytes = useLeftCurrentValue
                ? context.Value
                : otherValueBytes;
            var rightValueBytes = useLeftCurrentValue
                ? otherValueBytes
                : context.Value;

            var leftValue = await StreamSerialization.DeserializeAsync<TValue>(
                context.RequestServices,
                leftValueBytes,
                _leftTopicName,
                new Headers(),
                context.CancellationToken,
                ResolveFormat(_leftTopicName)).ConfigureAwait(false);

            var rightValue = await StreamSerialization.DeserializeAsync<TOtherValue>(
                context.RequestServices,
                rightValueBytes,
                _rightTopicName,
                new Headers(),
                context.CancellationToken,
                ResolveFormat(_rightTopicName)).ConfigureAwait(false);

            if (_innerJoin && (leftValue is null || rightValue is null))
            {
                return;
            }

            var key = await StreamSerialization.DeserializeAsync<TKey>(
                context.RequestServices,
                context.Key,
                currentTopic,
                context.Headers,
                context.CancellationToken,
                ResolveFormat(currentTopic)).ConfigureAwait(false);

            var joinedValues = (Item1: leftValue, Item2: rightValue);
            await processor(new StreamContext(context), key!, joinedValues).ConfigureAwait(false);
        }

        private string? ResolveFormat(string topicName)
        {
            return _registry.Topics.TryGetValue(topicName, out var registration)
                ? registration.Format
                : null;
        }
    }
}
