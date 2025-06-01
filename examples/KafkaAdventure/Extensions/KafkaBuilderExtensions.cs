using MinimalKafka.Builders;
using MinimalKafka.Extension;

namespace KafkaAdventure.Extensions;

public static class KafkaBuilderExtensions
{
    public static IKafkaConventionBuilder AsFeature(this IKafkaConventionBuilder builder, string featureName)
    {
        builder.WithClientId(featureName);
        builder.WithGroupId(featureName + "-" + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        return builder;
    }
}
