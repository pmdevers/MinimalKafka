using Confluent.Kafka;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;

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
    /// Thrown if the metadata does not contain an <see cref="IConfigMetadata"/> entry.
    /// </exception>
    public static ConsumerConfig ConsumerConfig(this IReadOnlyList<object> metadata) 
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ConsumerConfig
        ?? throw new InvalidOperationException("No IConfigMetadata found in builder metadata.");

    /// <summary>
    /// Retrieves <see cref="IConsumerHandlerMetadata"/> describing registered consumer handlers from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="IConsumerHandlerMetadata"/> instance if present; otherwise a new (empty) <see cref="ConsumerHandlerMetadata"/>.
    /// </returns>
    public static ProducerConfig ProducerConfig(this IReadOnlyList<object> metadata)
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ProducerConfig
        ?? throw new InvalidOperationException("No IConfigMetadata found in builder metadata.");

    /// <summary>
    /// Retrieves <see cref="IConsumerHandlerMetadata"/> describing registered consumer handlers from the specified metadata collection.
    /// </summary>
    /// <param name="metadata">
    /// The metadata collection, usually from a Kafka convention or builder.
    /// </param>
    /// <returns>
    /// The <see cref="IConsumerHandlerMetadata"/> instance if present; otherwise a new (empty) <see cref="ConsumerHandlerMetadata"/>.
    /// </returns>
    public static IConsumerHandlerMetadata ConsumerHandlers(this IReadOnlyList<object> metadata)
        => metadata.OfType<IConsumerHandlerMetadata>().FirstOrDefault() ??
            new ConsumerHandlerMetadata();

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
        => metadata.OfType<IReportIntervalMetadata>().FirstOrDefault()?.ReportInterval ?? 5;

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
        => metadata.OfType<IConfigMetadata>().FirstOrDefault()?.ConsumerConfig.EnableAutoCommit ?? true;
}

    