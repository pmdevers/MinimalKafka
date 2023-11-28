namespace MinimalKafka;

public sealed class TopicHandlerBuilder : ITopicConventionBuilder
{
    private readonly ICollection<Action<ITopicConsumerBuilder>> _conventions;
    private readonly ICollection<Action<ITopicConsumerBuilder>> _finallyConventions;

    public TopicHandlerBuilder(
        ICollection<Action<ITopicConsumerBuilder>> conventions,
        ICollection<Action<ITopicConsumerBuilder>> finallyConventions)
    {
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    public void Add(Action<ITopicConsumerBuilder> convention)
    {
        _conventions.Add(convention);
    }

    public void Finally(Action<ITopicConsumerBuilder> finalConvention)
    {
        _finallyConventions.Add(finalConvention);
    }
}
