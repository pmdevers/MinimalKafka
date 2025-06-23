using MinimalKafka.Builders;
using MinimalKafka.Extension;

namespace KafkaAdventure.Extensions;

public static class KafkaBuilderExtensions
{
    /// <summary>
    /// Configures the Kafka convention builder with a client ID set to the specified feature name and a group ID combining the feature name and the current ASP.NET Core environment.
    /// </summary>
    /// <param name="featureName">The name to use as the Kafka client ID and as part of the group ID.</param>
    /// <returns>The modified <see cref="IKafkaConventionBuilder"/> instance.</returns>
    public static IKafkaConventionBuilder AsFeature(this IKafkaConventionBuilder builder, string featureName)
    {
        builder.WithClientId(featureName);
        builder.WithGroupId(featureName + "-" + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        return builder;
    }
}
