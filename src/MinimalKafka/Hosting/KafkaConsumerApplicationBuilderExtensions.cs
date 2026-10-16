using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka;

/// <summary>Extension methods for mapping Kafka topics in an application builder.</summary>
public static class KafkaConsumerApplicationBuilderExtensions
{
    extension<T>(T builder)
        where T : IApplicationBuilder
    {
        /// <summary>
        /// Registers a topic handler and returns a builder for topic-specific configuration.
        /// </summary>
        /// <param name="topic">The Kafka topic name.</param>
        /// <param name="handler">The delegate that handles consumed messages for the topic.</param>
        /// <returns>A <see cref="TopicBuilder"/> for further topic configuration.</returns>
        public TopicBuilder MapTopic(string topic, Delegate handler)
        {
            var topicName = builder.ApplicationServices.GetRequiredService<ITopicNamingConvention>().Apply(topic);
            var registration = builder.ApplicationServices.GetRequiredService<TopicRegistry>()
                .Add(topicName, handler);

            return new TopicBuilder(registration);
        }
    }
}
