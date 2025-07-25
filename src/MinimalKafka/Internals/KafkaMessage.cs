﻿using Confluent.Kafka;
using System.Text;

namespace MinimalKafka.Internals;

internal record KafkaMessage()
{
    public required string Topic { get; init; }
    public required byte[] Key { get; init; }
    public required byte[] Value { get; init; }
    public required Dictionary<string, string> Headers { get; init; }
    internal static Headers GetKafkaHeaders() => [];
        
};
