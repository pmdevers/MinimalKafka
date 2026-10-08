using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;

namespace MinimalKafka.Hosting;

internal sealed class KafkaConsumerBackgroundService(
    TopicRegistry registry,
    IOptions<KafkaConsumerOptions> options,
    IServiceScopeFactory scopeFactory,
    IMessageProducer producer,
    ILogger<KafkaConsumerBackgroundService> logger) : BackgroundService
{
    private readonly TopicRegistry _registry = registry;
    private readonly IOptions<KafkaConsumerOptions> _options = options;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMessageProducer _producer = producer;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger = logger;

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

        using var consumer = CreateConsumer();
        var topics = ResolveTopics();

        consumer.Subscribe(topics.Keys);
        _logger.LogInformation("Subscribed Kafka consumer to {Topics}.", string.Join(", ", topics.Keys));

#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
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
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(ex, "Kafka consumer stopped OperationCancleled.");
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
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    private IConsumer<string, byte[]> CreateConsumer()
    {
        var handlers = _options.Value.Handlers;

        return new ConsumerBuilder<string, byte[]>(_options.Value.CreateConsumerConfig())
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetStatisticsHandler((consumer, statistics) => handlers.StatisticsHandler?.Invoke(consumer, statistics))
            .SetErrorHandler((consumer, error) =>
            {

                if (handlers.ErrorHandler == null)
                {
                    var message = $"[{GetLocal(error)}] {error.Code} - {error.Reason}";
                    _logger.LogError(message);

                    static string GetLocal(Error e)
                    {
                        string logVar = string.Empty;
                        if (e.IsLocalError)
                        {
                            logVar = $"{logVar}LOCAL";
                        }
                        if (e.IsBrokerError)
                        {
                            logVar = $"{logVar}BROKER";
                        }

                        return logVar;
                    }
                }

                handlers.ErrorHandler?.Invoke(consumer, error);
            })

            .SetLogHandler((consumer, logMessage) => handlers.LogHandler?.Invoke(consumer, logMessage))
            .SetPartitionsAssignedHandler((consumer, partitions) => handlers.PartitionsAssignedHandler?.Invoke(consumer, partitions))
            .SetPartitionsLostHandler((consumer, partitions) => handlers.PartitionsLostHandler?.Invoke(consumer, partitions))
            .SetPartitionsRevokedHandler((consumer, partitions) => handlers.PartitionsRevokedHandler?.Invoke(consumer, partitions))
            .SetOAuthBearerTokenRefreshHandler((consumer, config) => handlers.OAuthBearerTokenRefreshHandler?.Invoke(consumer, config))
            .Build();
    }

    private Dictionary<string, TopicRegistration> ResolveTopics()
    {
        var topics = new Dictionary<string, TopicRegistration>(StringComparer.Ordinal);
        foreach (var registration in _registry.Topics.Values)
        {
            var name = registration.Topic;
            if (!topics.TryAdd(registration.Topic, registration))
            {
                throw new InvalidOperationException(
                    $"Topics '{topics[name].Topic}' and '{registration.Topic}' both resolve to Kafka topic '{name}'.");
            }
        }
        return topics;
    }
}
