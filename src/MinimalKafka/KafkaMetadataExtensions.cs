using Confluent.Kafka;
using MinimalKafka.Metadata;

namespace MinimalKafka;

/// <summary>
/// Extension methods for extracting common Kafka configuration and handler metadata
/// from metadata collections, typically produced by MinimalKafka builders.
/// </summary>
public static class KafkaMetadataExtensions
{
    /// <summary>
    /// Retrieves the <see cref="ConsumerConfig"/> from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="ConsumerConfig"/> if present in the metadata; otherwise throws <see cref="InvalidOperationException"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the metadata does not contain an <see cref="ConfigMetadataAttribute"/> entry.
    /// </exception>
    public static ConsumerConfig ConsumerConfig(this IReadOnlyList<object> metadata)
        => metadata.OfType<ConfigMetadataAttribute>().FirstOrDefault()?.BuildConsumerConfig()
        ?? throw new InvalidOperationException("No ConfigMetadataAttribute found in builder metadata.");

    /// <summary>
    /// Retrieves the <see cref="ProducerConfig"/> from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="ProducerConfig"/> if present in the metadata; otherwise throws <see cref="InvalidOperationException"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the metadata does not contain an <see cref="ConfigMetadataAttribute"/> entry.
    /// </exception>
    public static ProducerConfig ProducerConfig(this IReadOnlyList<object> metadata)
        => metadata.OfType<ConfigMetadataAttribute>().FirstOrDefault()?.BuildProducerConfig()
        ?? throw new InvalidOperationException("No ConfigMetadataAttribute found in builder metadata.");

    /// <summary>
    /// Retrieves <see cref="ConsumerHandlerMetadataAttribute"/> describing registered consumer handlers from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="ConsumerHandlerMetadataAttribute"/> instance if present; otherwise a new (empty) <see cref="ConsumerHandlerMetadataAttribute"/>.
    /// </returns>
    public static ConsumerHandlerMetadataAttribute ConsumerHandlers(this IReadOnlyList<object> metadata)
        => metadata.OfType<ConsumerHandlerMetadataAttribute>().FirstOrDefault() ??
            new ConsumerHandlerMetadataAttribute();

    /// <summary>
    /// Retrieves <see cref="ProducerHandlerMetadataAttribute"/> describing registered producer handlers from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="ProducerHandlerMetadataAttribute"/> instance if present; otherwise a new (empty) <see cref="ProducerHandlerMetadataAttribute"/>.
    /// </returns>
    public static ProducerHandlerMetadataAttribute ProducerHandlers(this IReadOnlyList<object> metadata)
        => metadata.OfType<ProducerHandlerMetadataAttribute>().FirstOrDefault() ??
            new ProducerHandlerMetadataAttribute();

    /// <summary>
    /// Gets the reporting interval (in seconds) for Kafka metrics or consumer activity.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The reporting interval (in seconds) if found; otherwise 5 seconds.
    /// </returns>
    public static int ReportInterval(this IReadOnlyList<object> metadata)
        => metadata.OfType<ReportIntervalMetadataAttribute>().FirstOrDefault()?.ReportInterval ?? 5;

    /// <summary>
    /// Indicates whether automatic offset commit is enabled for the consumer.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// <c>true</c> if auto commit is enabled or not specified; <c>false</c> otherwise.
    /// </returns>
    public static bool AutoCommitEnabled(this IReadOnlyList<object> metadata)
        => metadata.OfType<ConfigMetadataAttribute>().FirstOrDefault()?.BuildConsumerConfig().EnableAutoCommit ?? true;
}

