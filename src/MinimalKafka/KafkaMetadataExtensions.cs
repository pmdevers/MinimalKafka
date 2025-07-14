using Confluent.Kafka;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public static class KafkaMetadataExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static ConsumerConfig ConsumerConfig(this IReadOnlyList<object> metadata) 
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ConsumerConfig
        ?? throw new InvalidOperationException("No IConfigMetadata found in builder metadata.");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static ProducerConfig ProducerConfig(this IReadOnlyList<object> metadata)
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ProducerConfig
        ?? throw new InvalidOperationException("No IConfigMetadata found in builder metadata.");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static IConsumerHandlerMetadata ConsumerHandlers(this IReadOnlyList<object> metadata)
        => metadata.OfType<IConsumerHandlerMetadata>().FirstOrDefault() ??
            new ConsumerHandlerMetadata();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static int ReportInterval(this IReadOnlyList<object> metadata)
        => metadata.OfType<IReportIntervalMetadata>().FirstOrDefault()?.ReportInterval ?? 5;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static bool AutoCommitEnabled(this IReadOnlyList<object> metadata)
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ConsumerConfig.EnableAutoCommit ?? true;
}

    