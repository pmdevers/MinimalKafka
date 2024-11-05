#pragma warning disable S2326

using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;

public interface ITopicMetadata
{
    Func<Type, string> NamingConvention { get; }
    
    TimeSpan? RetentionPeriod { get; }
}

public interface ITopicMetadata<T> : ITopicMetadata
{
}

public class TopicMetadata(Func<Type, string> namingConvention, TimeSpan? retentionPeriod) : ITopicMetadata
{
    public Func<Type, string> NamingConvention { get; } = namingConvention;

    public TimeSpan? RetentionPeriod { get; } = retentionPeriod;

    /// <inheritdoc/>
    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(TopicMetadata), NamingConvention);
}

public class TopicMetadata<T>(Func<Type, string> namingConvention, TimeSpan? retentionPeriod) :
    TopicMetadata(namingConvention, retentionPeriod), ITopicMetadata<T>;