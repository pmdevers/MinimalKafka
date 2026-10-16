using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;

namespace MinimalKafka.Hosting;

internal sealed class KafkaConsumerBackgroundService(
    TopicRegistry registry,
    IKafkaConsumerFactory consumerFactory,
    IServiceScopeFactory scopeFactory,
    IKafkaProducer producer,
    IConsumerRuntimeControl runtimeControl,
    ILogger<KafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan _consumePollingInterval = TimeSpan.FromMilliseconds(250);
    private readonly TopicRegistry _registry = registry;
    private readonly IKafkaConsumerFactory _consumerFactory = consumerFactory;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IKafkaProducer _producer = producer;
    private readonly IConsumerRuntimeControl _runtimeControl = runtimeControl;
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

        var topics = ResolveTopics();
        IKafkaConsumer? consumer = null;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_runtimeControl.IsRunning)
                {
                    if (consumer is not null)
                    {
                        consumer.Close();
                        consumer.Dispose();
                        consumer = null;
                        _logger.LogInformation("Kafka consumer processing stopped and left consumer group.");
                    }

                    await Task.Delay(_consumePollingInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                if (consumer is null)
                {
                    consumer = _consumerFactory.Create();
                    consumer.Subscribe(topics.Keys);
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Subscribed Kafka consumer to topics:{Topics}",
                            Environment.NewLine + "  - " + string.Join(Environment.NewLine + "  - ", topics.Keys));
                    }
                }

                var result = consumer.Consume(_consumePollingInterval);
                if (result is null)
                {
                    continue;
                }

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
            _logger.LogInformation(ex, "Kafka consumer stopped OperationCanceled.");
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical(exception, "Kafka consumer stopped after message processing failed.");
            }
            throw;
        }
        finally
        {
            if (consumer is not null)
            {
                consumer.Close();
                consumer.Dispose();
            }
        }
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
