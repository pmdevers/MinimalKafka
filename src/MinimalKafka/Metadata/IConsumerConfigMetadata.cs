using Confluent.Kafka;

namespace MinimalKafka.Metadata;

/// <summary>
/// 
/// </summary>
public interface IConfigMetadata
{

    /// <summary>
    /// 
    /// </summary>
    ConsumerConfig ConsumerConfig { get; }

    /// <summary>
    /// 
    /// </summary>
    ProducerConfig ProducerConfig { get; }

}