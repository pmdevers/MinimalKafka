using Microsoft.Extensions.DependencyInjection;
using MinimalKafka;

namespace Microsoft.AspNetCore.Builder;

public static class TopicConsumerBuilderExtensions
{
    public static ITopicConventionBuilder MapTopic(this IApplicationBuilder builder, string topic, Delegate handler)
    {
        var tb = builder.ApplicationServices.GetRequiredService<ITopicConsumerBuilder>();
        return tb.MapTopic(topic, handler);
    }

    public static ITopicConventionBuilder MapTopic(this ITopicConsumerBuilder builder, string topic, Delegate handler)
    {
        return builder.GetOrAddTopicDataSource().AddTopicDelegate(topic, handler);
    }

    public static TBuilder WithMetaData<TBuilder>(this TBuilder builder, params object[] items) where TBuilder : ITopicConventionBuilder
    {
        builder.Add(b =>
        {
            foreach (var item in items)
            {
                b.Metadata.Add(item);
            }
        });

        return builder;
    }

    private static TopicDataSource GetOrAddTopicDataSource(this ITopicConsumerBuilder builder)
    {
        if (builder.DataSource is null)
        {
            builder.DataSource = new TopicDataSource(builder.ServiceProvider);
        }

        return builder.DataSource;
    }


}
