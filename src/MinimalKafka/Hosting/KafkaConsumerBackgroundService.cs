using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;

namespace MinimalKafka.Hosting;

internal sealed class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly TopicRegistry _registry;
    private readonly IOptions<KafkaConsumerOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageProducer _producer;
    private readonly ITopicNamingConvention? _naming;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;

    public KafkaConsumerBackgroundService(
        TopicRegistry registry,
        IOptions<KafkaConsumerOptions> options,
        IServiceScopeFactory scopeFactory,
        IMessageProducer producer,
        IEnumerable<ITopicNamingConvention> naming,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _registry = registry;
        _options = options;
        _scopeFactory = scopeFactory;
        _producer = producer;
        _naming = naming.LastOrDefault();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _registry.Topics.Count == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ConfigureAwait(false);
        }

        if (_registry.Topics.Count == 0)
        {
            return;
        }

        using var consumer = new ConsumerBuilder<string, byte[]>(_options.Value.CreateConsumerConfig())
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();
        var topics = ResolveTopics();
        consumer.Subscribe(topics.Keys);
        _logger.LogInformation("Subscribed Kafka consumer to {Topics}.", string.Join(", ", topics.Keys));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                if (!topics.TryGetValue(result.Topic, out var registration))
                {
                    _logger.LogWarning("Received a message for unmapped Kafka topic {Topic}.", result.Topic);
                    continue;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = new KafkaContext(result, _producer, scope.ServiceProvider, stoppingToken)
                {
                    Format = registration.Format
                };
                var middleware = scope.ServiceProvider.GetServices<IConsumerMiddleware>()
                    .Concat(registration.Middleware.Select(create => create(scope.ServiceProvider)))
                    .ToArray();
                ConsumerDelegate pipeline = registration.Handler;
                for (var index = middleware.Length - 1; index >= 0; index--)
                {
                    var current = middleware[index];
                    var next = pipeline;
                    pipeline = kafkaContext => current.InvokeAsync(kafkaContext, next);
                }

                await pipeline(context).ConfigureAwait(false);
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Kafka consumer stopped after message processing failed.");
            throw;
        }
        finally
        {
            consumer.Close();
        }
    }

    private Dictionary<string, TopicRegistration> ResolveTopics()
    {
        var topics = new Dictionary<string, TopicRegistration>(StringComparer.Ordinal);
        foreach (var registration in _registry.Topics.Values)
        {
            var name = _naming?.Apply(registration.Topic) ?? registration.Topic;
            if (!topics.TryAdd(name, registration))
            {
                throw new InvalidOperationException(
                    $"Topics '{topics[name].Topic}' and '{registration.Topic}' both resolve to Kafka topic '{name}'.");
            }
        }
        return topics;
    }
}
