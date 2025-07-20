using KafkaAdventure.Domain;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features;

public static class LocationProcessor
{
    public static void MapLocations<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder
    {
        builder.MapStream<int, Location>("game-locations")
            .ToTable("locations");
    }
}
