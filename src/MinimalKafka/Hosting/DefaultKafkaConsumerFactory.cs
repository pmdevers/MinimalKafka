using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Hosting;

internal sealed class DefaultKafkaConsumerFactory(
    IOptions<KafkaConsumerOptions> options,
    ILogger<DefaultKafkaConsumerFactory> logger) : IKafkaConsumerFactory
{
    private readonly IOptions<KafkaConsumerOptions> _options = options;
    private readonly ILogger<DefaultKafkaConsumerFactory> _logger = logger;

    public IKafkaConsumer Create()
    {
        var handlers = _options.Value.Handlers;

        var consumer = new ConsumerBuilder<byte[], byte[]>(_options.Value.CreateConsumerConfig())
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetStatisticsHandler((innerConsumer, statistics) => handlers.StatisticsHandler?.Invoke(innerConsumer, statistics))
            .SetErrorHandler((innerConsumer, error) =>
            {
                if (handlers.ErrorHandler == null && _logger.IsEnabled(LogLevel.Error))
                {
                    var errorScope = GetErrorScope(error);
                    _logger.LogError("[{ErrorScope}] {ErrorCode} - {ErrorReason}", errorScope, error.Code, error.Reason);
                }

                handlers.ErrorHandler?.Invoke(innerConsumer, error);
            })
            .SetLogHandler((innerConsumer, logMessage) => handlers.LogHandler?.Invoke(innerConsumer, logMessage))
            .SetPartitionsAssignedHandler((innerConsumer, partitions) => handlers.PartitionsAssignedHandler?.Invoke(innerConsumer, partitions))
            .SetPartitionsLostHandler((innerConsumer, partitions) => handlers.PartitionsLostHandler?.Invoke(innerConsumer, partitions))
            .SetPartitionsRevokedHandler((innerConsumer, partitions) => handlers.PartitionsRevokedHandler?.Invoke(innerConsumer, partitions))
            .SetOAuthBearerTokenRefreshHandler((innerConsumer, config) => handlers.OAuthBearerTokenRefreshHandler?.Invoke(innerConsumer, config))
            .Build();

        return new KafkaConsumerAdapter(consumer);
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

internal sealed class KafkaConsumerAdapter(IConsumer<byte[], byte[]> innerConsumer) : IKafkaConsumer
{
    private readonly IConsumer<byte[], byte[]> _innerConsumer = innerConsumer;

    public void Subscribe(IEnumerable<string> topics) => _innerConsumer.Subscribe(topics);

    public ConsumeResult<byte[], byte[]>? Consume(TimeSpan timeout) => _innerConsumer.Consume(timeout);

    public void Commit(ConsumeResult<byte[], byte[]> result) => _innerConsumer.Commit(result);

    public void Close() => _innerConsumer.Close();

    public void Dispose() => _innerConsumer.Dispose();
}
