using Confluent.Kafka;
using MinimalKafka.Factory;
using System.Runtime.InteropServices;

namespace MinimalKafka;

public sealed class TopicDataSource
{
    private readonly List<TopicEntry> _topicEntries = new();

    public TopicDataSource(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    private struct TopicEntry
    {
        public string TopicName { get; init; }

        public Delegate TopicHandler { get; init; }

        public AddAfterTopicBuildConventionCollection Conventions { get; init; }

        public AddAfterTopicBuildConventionCollection FinallyConventions { get; init; }
    }

    private sealed class AddAfterTopicBuildConventionCollection :
        List<Action<ITopicConsumerBuilder>>, 
        ICollection<Action<ITopicConsumerBuilder>>
    {
        public bool IsReadOnly { get; set; }

        void ICollection<Action<ITopicConsumerBuilder>>.Add(Action<ITopicConsumerBuilder> convention)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("TopicDatasource can not be modified after build.");
            }

            Add(convention);
        }
    }

    public TopicHandlerBuilder AddTopicDelegate(string topicName, Delegate handler)
    {
        var conventions = new AddAfterTopicBuildConventionCollection();
        var finnalyConventions = new AddAfterTopicBuildConventionCollection();

        _topicEntries.Add(new()
        {
            TopicName = topicName,
            TopicHandler = handler,
            Conventions = conventions,
            FinallyConventions = finnalyConventions,
        });

        return new TopicHandlerBuilder(conventions, finnalyConventions);
    }

    public IEnumerable<Topic> GetTopics()
    {
        foreach (var item in _topicEntries)
        {
            var results = TopicDelegateFactory.Create(item.TopicHandler, new TopicDelegateFactoryOptions()
            {
                ServiceProvider = ServiceProvider,
            });

            yield return new Topic()
            {
                TopicHandler = results.TopicDelegate,
                TopicName = item.TopicName,
            };
        }
    }
}
