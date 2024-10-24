﻿namespace MinimalKafka.Stream;

public interface IStreamBuilder<K1, V1> : IIntoBuilder<K1, V1>, IWithMetadataBuilder
{
    IJoinBuilder<K1, V1, K2, V2> Join<K2, V2>(string topic);
}

public interface IWithMetadataBuilder
{
    IWithMetadataBuilder WithGroupId(string groupId);
    IWithMetadataBuilder WithClientId(string clientId);
}

