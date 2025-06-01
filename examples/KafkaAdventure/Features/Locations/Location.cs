namespace KafkaAdventure.Features.Locations;

public class Location
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string, string> Exits { get; set; }
}