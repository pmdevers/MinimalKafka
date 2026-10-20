namespace MinimalKafka.Dashboard.Domain.Models;

internal sealed record TopicPartition(string Topic, int Partition);
