﻿using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

internal class ValueDeserializerMetadata(Func<IKafkaConsumerBuilder, object> valueDeserializerType) : IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer => valueDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(ValueDeserializerMetadata), Deserializer);
}
