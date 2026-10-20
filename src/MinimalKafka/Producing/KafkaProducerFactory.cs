using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Producing;

internal sealed class KafkaProducerFactory(
    IOptions<KafkaOptions> options,
    ILogger<KafkaProducerFactory> logger) : IKafkaProducerFactory
{
    private readonly IOptions<KafkaOptions> _options = options;
    private readonly ILogger<KafkaProducerFactory> _logger = logger;

    public IProducer<byte[], byte[]> Create()
    {
        var handlers = _options.Value.Handlers;

        return new ProducerBuilder<byte[], byte[]>(_options.Value.CreateProducerConfig())
            .SetKeySerializer(Serializers.ByteArray)
            .SetValueSerializer(Serializers.ByteArray)
            .SetErrorHandler((innerProducer, error) =>
            {
                if (handlers.ErrorHandler == null && _logger.IsEnabled(LogLevel.Error))
                {
                    var errorScope = GetErrorScope(error);
                    _logger.LogError("[{ErrorScope}] {ErrorCode} - {ErrorReason}", errorScope, error.Code, error.Reason);
                }

                handlers.ErrorHandler?.Invoke(innerProducer, error);
            })
            .SetLogHandler((innerProducer, logMessage) => handlers.LogHandler?.Invoke(innerProducer, logMessage))
            .SetOAuthBearerTokenRefreshHandler((innerProducer, config) => handlers.OAuthBearerTokenRefreshHandler?.Invoke(innerProducer, config))
            .SetStatisticsHandler((innerProducer, statistics) => handlers.StatisticsHandler?.Invoke(innerProducer, statistics))
            .Build();
    }

    private static string GetErrorScope(Error error)
    {
        string scope = string.Empty;
        if (error.IsLocalError)
        {
            scope = $"{scope}LOCAL";
        }

        if (error.IsBrokerError)
        {
            scope = $"{scope}BROKER";
        }

        return scope;
    }
}
