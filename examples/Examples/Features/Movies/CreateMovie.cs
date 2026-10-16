using Examples.Domain;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.Movies;

public static class CreateMovie
{
    public const string Route = "/";

    public record Request(string Name, Genre Genre);

    public static async Task<IResult> Handle(
        [FromServices] IKafkaProducer producer,
        [FromBody] Request request,
        CancellationToken cancellationToken
        )
    {
        var movie = new Movie()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Genre = request.Genre,
        };

        await producer.ProduceAsync("movies", movie.Id, movie, cancellationToken: cancellationToken);

        return TypedResults.Ok(movie);
    }
}
