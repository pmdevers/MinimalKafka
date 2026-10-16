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

    /// <summary>Uses a value predicate as the join condition.</summary>
    public PredicateJoinedStreamBuilder On(Func<TValue, TOtherValue, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new PredicateJoinedStreamBuilder(_registry, _leftTopicName, _rightTopicName, _innerJoin, predicate);
    }

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
                    ResolveFormat(currentTopic),
                    context.CancellationToken).ConfigureAwait(false);
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
                [],
                ResolveFormat(_leftTopicName),
                context.CancellationToken).ConfigureAwait(false);

            var rightValue = await StreamSerialization.DeserializeAsync<TOtherValue>(
                context.RequestServices,
                rightValueBytes,
                _rightTopicName,
                [],
                ResolveFormat(_rightTopicName),
                context.CancellationToken).ConfigureAwait(false);

            if (_innerJoin && (leftValue is null || rightValue is null))
            {
                return;
            }

            var key = await StreamSerialization.DeserializeAsync<TKey>(
                context.RequestServices,
                context.Key,
                currentTopic,
                context.Headers,
                ResolveFormat(currentTopic),
                context.CancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// Terminal builder for a predicate-based join.
    /// </summary>
    public sealed class PredicateJoinedStreamBuilder
    {
        private readonly TopicRegistry _registry;
        private readonly string _leftTopicName;
        private readonly string _rightTopicName;
        private readonly bool _innerJoin;
        private readonly Func<TValue, TOtherValue, bool> _predicate;

        internal PredicateJoinedStreamBuilder(
            TopicRegistry registry,
            string leftTopicName,
            string rightTopicName,
            bool innerJoin,
            Func<TValue, TOtherValue, bool> predicate)
        {
            _registry = registry;
            _leftTopicName = leftTopicName;
            _rightTopicName = rightTopicName;
            _innerJoin = innerJoin;
            _predicate = predicate;
        }

        /// <summary>Registers a join processor that receives the left key and both joined values.</summary>
        public void Into(Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            ArgumentNullException.ThrowIfNull(processor);
            EnsureKeyTypesMatch();
            Register(true, async (streamContext, key, values) => await processor(streamContext, key, values).ConfigureAwait(false));
        }

        /// <summary>Registers a join processor that receives only the joined values.</summary>
        public void Into(Func<StreamContext, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            ArgumentNullException.ThrowIfNull(processor);
            Register(false, async (streamContext, _, values) => await processor(streamContext, values).ConfigureAwait(false));
        }

        private void EnsureKeyTypesMatch()
        {
            if (typeof(TKey) != typeof(TOtherKey))
            {
                throw new InvalidOperationException(
                    $"Predicate join overload with a key parameter requires both topics to use the same key type, but '{_leftTopicName}' uses '{typeof(TKey).FullName}' and '{_rightTopicName}' uses '{typeof(TOtherKey).FullName}'.");
            }
        }

        private void Register(bool passKey, Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            _registry.Add(_leftTopicName, (Func<KafkaContext, Task>)(async context =>
            {
                await StreamSourceBuilder<TKey, TValue>.StoreCurrentMessageAsync(context).ConfigureAwait(false);
                await InvokeProcessorAsync(
                    context,
                    currentTopic: _leftTopicName,
                    otherTopic: _rightTopicName,
                    useLeftCurrentValue: true,
                    passKey,
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
                    passKey,
                    processor).ConfigureAwait(false);
            }));
        }

        private async Task InvokeProcessorAsync(
            KafkaContext context,
            string currentTopic,
            string otherTopic,
            bool useLeftCurrentValue,
            bool passKey,
            Func<StreamContext, TKey, (TValue? Item1, TOtherValue? Item2), Task> processor)
        {
            var key = passKey
                ? await StreamSerialization.DeserializeAsync<TKey>(
                    context.RequestServices,
                    context.Key,
                    currentTopic,
                    context.Headers,
                    ResolveFormat(currentTopic),
                    context.CancellationToken).ConfigureAwait(false)
                : default;

            var currentLeftValue = useLeftCurrentValue
                ? await StreamSerialization.DeserializeAsync<TValue>(
                    context.RequestServices,
                    context.Value,
                    _leftTopicName,
                    context.Headers,
                    ResolveFormat(_leftTopicName),
                    context.CancellationToken).ConfigureAwait(false)
                : default;

            var currentRightValue = useLeftCurrentValue
                ? default
                : await StreamSerialization.DeserializeAsync<TOtherValue>(
                    context.RequestServices,
                    context.Value,
                    _rightTopicName,
                    context.Headers,
                    ResolveFormat(_rightTopicName),
                    context.CancellationToken).ConfigureAwait(false);

            var storeFactory = context.RequestServices.GetRequiredService<IKafkaStoreFactory>();
            var otherStore = storeFactory.GetStore(otherTopic);
            var streamContext = new StreamContext(context);
            var hasMatch = false;

            await foreach (var otherValueBytes in otherStore.GetItems())
            {
                if (useLeftCurrentValue)
                {
                    var otherRightValue = await StreamSerialization.DeserializeAsync<TOtherValue>(
                        context.RequestServices,
                        otherValueBytes,
                        _rightTopicName,
                        [],
                        ResolveFormat(_rightTopicName),
                        context.CancellationToken).ConfigureAwait(false);

                    if (currentLeftValue is null || otherRightValue is null || !_predicate(currentLeftValue, otherRightValue))
                    {
                        continue;
                    }

                    hasMatch = true;
                    await processor(streamContext, key!, (currentLeftValue, otherRightValue)).ConfigureAwait(false);
                    continue;
                }

                var otherLeftValue = await StreamSerialization.DeserializeAsync<TValue>(
                    context.RequestServices,
                    otherValueBytes,
                    _leftTopicName,
                    [],
                    ResolveFormat(_leftTopicName),
                    context.CancellationToken).ConfigureAwait(false);

                if (otherLeftValue is null || currentRightValue is null || !_predicate(otherLeftValue, currentRightValue))
                {
                    continue;
                }

                hasMatch = true;
                await processor(streamContext, key!, (otherLeftValue, currentRightValue)).ConfigureAwait(false);
            }

            if (hasMatch || _innerJoin)
            {
                return;
            }

            var joinedValues = useLeftCurrentValue
                ? (Item1: currentLeftValue, Item2: default(TOtherValue))
                : (Item1: default, Item2: currentRightValue);

            await processor(streamContext, key!, joinedValues).ConfigureAwait(false);
        }

        private string? ResolveFormat(string topicName)
        {
            return _registry.Topics.TryGetValue(topicName, out var registration)
                ? registration.Format
                : null;
        }
    }
}
