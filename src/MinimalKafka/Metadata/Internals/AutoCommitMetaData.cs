using Confluent.Kafka;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class AutoCommitMetaDataAttribute(bool enabled) : Attribute, IAutoCommitMetaData
{
    public bool Enabled { get; } = enabled;

    public void Set(ClientConfig config)
    {
        config.Set("enable.auto.commit", Enabled ? "true" : "false");
        config.Set("enable.auto.offset.store", Enabled ? "true" : "false");
    }
}
