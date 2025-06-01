using System.Reflection;
using System.Text.Json;

namespace KafkaAdventure.Features.Locations;

public class LocationContext
{
    public LocationContext()
    {
        Locations = [
            new Location() {
                Id = 1,
                Name = "The Forest",
                Description = "You are in a dark forest. The trees are tall and the air is damp.",
                Exits = new Dictionary<string, string> {
                    { "north", "The Forest" },
                    { "south", "The Forest" },
                    { "east", "The Forest" },
                    { "west", "The Forest" }
                }
            }];

    }

    public List<Location> Locations { get; set; }

   
}