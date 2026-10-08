using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public static class KafkaConsumerApplicationBuilderExtensions
{
    extension(IApplicationBuilder builder)
    {
        /// <summary>
        /// Registers a topic handler and returns a builder for topic-specific configuration.
        /// </summary>
        /// <param name="topic">The Kafka topic name.</param>
        /// <param name="handler">The delegate that handles consumed messages for the topic.</param>
        /// <returns>A <see cref="TopicBuilder"/> for further topic configuration.</returns>
        public TopicBuilder MapTopic(string topic, Delegate handler)
        {
            var registration = builder.ApplicationServices.GetRequiredService<TopicRegistry>()
                .Add(topic, handler);

            return new TopicBuilder(registration);
        }
    }
}
