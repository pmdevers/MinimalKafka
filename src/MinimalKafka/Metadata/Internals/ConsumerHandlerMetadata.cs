﻿using Confluent.Kafka;

namespace MinimalKafka.Metadata.Internals;

internal class ConsumerHandlerMetadata : IConsumerHandlerMetadata
{
    public Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; set; }
    public Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>? PartitionsLostHandler { get; set; }
    public Action<object, List<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }
    public Action<object, string>? StatisticsHandler { get; set; }
    public Action<object, Error>? ErrorHandler { get; set; }
    public Action<object, LogMessage>? LogHandler { get; set; }
}
