using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;
public static class TopicHandlerBuilderExtensions
{
    public static ITopicHandlerBuilder MapTopic(
        this ITopicHandlerBuilder builder,
        [StringSyntax("Topic")] string pattern,
        Delegate topicDelegate)
    {
        builder.DataSources.Add()
    }
}
