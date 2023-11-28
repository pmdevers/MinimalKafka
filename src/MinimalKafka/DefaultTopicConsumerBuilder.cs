using Microsoft.AspNetCore.Builder;

namespace MinimalKafka;

internal sealed class DefaultTopicConsumerBuilder : ITopicConsumerBuilder
{
    public DefaultTopicConsumerBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public TopicDataSource? DataSource { get; set; }

    public List<object> Metadata { get; } = new List<object>();

    //public TopicConsumer Build()
    //{
    //    return new();
    //}
}
