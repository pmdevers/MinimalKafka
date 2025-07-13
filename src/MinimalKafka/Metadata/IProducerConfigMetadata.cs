using Confluent.Kafka;

namespace MinimalKafka.Metadata;

/// <summary>
/// 
/// </summary>
public interface IProducerConfigMetadata
{
    /// <summary>
    /// 
    /// </summary>
    ProducerConfig Config { get; }    
}