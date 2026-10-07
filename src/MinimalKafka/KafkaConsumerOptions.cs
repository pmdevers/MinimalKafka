using Confluent.Kafka;

namespace MinimalKafka;

/// <summary>
/// Raw librdkafka configuration (https://github.com/confluentinc/librdkafka/blob/master/CONFIGURATION.md),
/// so every Kafka setting is supported by its property name, e.g. "security.protocol".
/// </summary>
public sealed class KafkaConsumerOptions
{
    /// <summary>Settings applied to both the consumer and the producer, such as bootstrap.servers and security.*.</summary>
    public Dictionary<string, string> Common { get; } = new(StringComparer.Ordinal)
    {
        ["bootstrap.servers"] = "localhost:9092"
    };

    /// <summary>Consumer-only settings. These override <see cref="Common"/>.</summary>
    public Dictionary<string, string> Consumer { get; } = new(StringComparer.Ordinal)
    {
        ["group.id"] = "kafka-consumer",
        ["auto.offset.reset"] = "earliest"
    };

    /// <summary>Producer-only settings. These override <see cref="Common"/>.</summary>
    public Dictionary<string, string> Producer { get; } = new(StringComparer.Ordinal);

    internal ConsumerConfig CreateConsumerConfig()
    {
        var config = new ConsumerConfig(Merge(Consumer));
        // Offsets are committed explicitly after a message is handled successfully.
        config.EnableAutoCommit = false;
        config.EnableAutoOffsetStore = false;
        return config;
    }

    internal ProducerConfig CreateProducerConfig() => new(Merge(Producer));

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