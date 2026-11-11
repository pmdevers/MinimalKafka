namespace Examples.Domain;

public class Movie
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Genre Genre { get; set; }
}
