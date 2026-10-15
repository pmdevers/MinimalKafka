using Confluent.Kafka;

namespace MinimalKafka;

/// <summary>
/// Raw librdkafka configuration (https://github.com/confluentinc/librdkafka/blob/master/CONFIGURATION.md),
/// so every Kafka setting is supported by its property name, e.g. "security.protocol".
/// </summary>
public sealed class KafkaConsumerOptions
{
    /// <summary>Settings applied to both the consumer and the producer, such as bootstrap.servers and security.*.</summary>
    public Dictionary<string, string> Common { get; set; } = new ClientConfig().ToDictionary();

    /// <summary>Consumer-only settings. These override <see cref="Common"/>.</summary>
    public Dictionary<string, string> Consumer { get; set; } = new ConsumerConfig()
    {
        GroupId = AppDomain.CurrentDomain.FriendlyName,
        ClientId = Environment.MachineName
    }.ToDictionary();

    /// <summary>Producer-only settings. These override <see cref="Common"/>.</summary>
    public Dictionary<string, string> Producer { get; set; } = new ProducerConfig()
        .ToDictionary();

    internal KafkaConsumerHandlers Handlers { get; } = new();

    /// <summary>
    /// Creates a <see cref="ConsumerConfig"/> instance by merging the <see cref="Common"/> and <see cref="Consumer"/> settings.
    /// </summary>
    public ConsumerConfig CreateConsumerConfig()
    {
        var config = new ConsumerConfig(Merge(Consumer.ToDictionary()))
        {
            // Offsets are committed explicitly after a message is handled successfully.
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };
        return config;
    }

    /// <summary>
    /// Creates a <see cref="ProducerConfig"/> instance by merging the <see cref="Common"/> and <see cref="Producer"/> settings.
    /// </summary>
    public ProducerConfig CreateProducerConfig() => new(Merge(Producer.ToDictionary()));

    /// <summary>
    /// Creates an <see cref="AdminClientConfig"/> instance by merging the <see cref="Common"/> and <see cref="Consumer"/> settings.
    /// </summary>
    public AdminClientConfig CreateAdminConfig() => new(Merge(Consumer.ToDictionary()));

    private Dictionary<string, string> Merge(Dictionary<string, string> specific)
    {
        var merged = new Dictionary<string, string>(Common, StringComparer.Ordinal);
        foreach (var (key, value) in specific)
        {
            merged[key] = value;
        }
        return merged;
    }
}

internal sealed class KafkaConsumerHandlers
{
    public Action<IConsumer<byte[], byte[]>, string>? StatisticsHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, Error>? ErrorHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, LogMessage>? LogHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, List<TopicPartition>>? PartitionsAssignedHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>>? PartitionsLostHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }

    public Action<IConsumer<byte[], byte[]>, string>? OAuthBearerTokenRefreshHandler { get; set; }
}
