﻿using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

public interface IStreamBuilder<K1, V1> : IKafkaConventionBuilder, IIntoBuilder<K1, V1>
{
    IJoinBuilder<K1, V1, K2, V2> Join<K2, V2>(string topic);
}

