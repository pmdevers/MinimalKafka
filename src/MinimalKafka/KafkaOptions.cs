using Confluent.Kafka;

namespace MinimalKafka;

/// <summary>
/// Raw librdkafka configuration (https://github.com/confluentinc/librdkafka/blob/master/CONFIGURATION.md),
/// so every Kafka setting is supported by its property name, e.g. "security.protocol".
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Indicates whether the Kafka consumer should start automatically when the application starts.
    /// </summary>
    public bool StartAtStartup { get; set; } = true;

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

    /// <summary>
    /// Contains handlers for various Kafka client events, such as statistics, errors, logs, and partition assignment/loss/revocation.
    /// </summary>
    public KafkaConsumerHandlers Handlers { get; } = new();

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
    public AdminClientConfig CreateAdminConfig() => new(Merge(Producer.ToDictionary()));

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

/// <summary>
/// Represents optional callback handlers for Kafka consumer client events, including statistics, errors, logs,
/// partition assignment changes, and OAuth bearer token refresh.
/// </summary>
/// <remarks>Assign delegates to receive event notifications during consumer operation. Any handler left <see
/// langword="null"/> is not invoked.</remarks>
public sealed class KafkaConsumerHandlers
{
    /// <summary>
    /// Gets or sets the callback invoked to handle statistics output for a client.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to disable statistics handling.</remarks>
    public Action<IClient, string>? StatisticsHandler { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an error is reported for a client.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to disable custom error handling.</remarks>
    public Action<IClient, Error>? ErrorHandler { get; set; }

    /// <summary>
    /// Gets or sets the delegate that handles client log messages.
    /// </summary>
    public Action<IClient, LogMessage>? LogHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when topic partitions are assigned to the client.
    /// </summary>
    /// <remarks>Use this callback to run custom logic during partition assignment.</remarks>
    public Action<IClient, List<TopicPartition>>? PartitionsAssignedHandler { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when assigned partitions are lost.
    /// </summary>
    public Action<IClient, List<TopicPartitionOffset>>? PartitionsLostHandler { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when assigned partitions are revoked.
    /// </summary>
    public Action<IClient, List<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when an OAuth bearer token needs to be refreshed.
    /// </summary>
    public Action<IClient, string>? OAuthBearerTokenRefreshHandler { get; set; }
}
