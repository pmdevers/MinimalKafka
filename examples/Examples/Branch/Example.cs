using Microsoft.AspNetCore.Mvc;
using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.Branch;

public enum Genre
{
    Drama,
    Horror,
    Fantasy
}

public record Movie(Guid Id, string Title, Genre Genre);

public static class Example
{
    public static void MapBranchExample<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder, IEndpointRouteBuilder
    {
        builder.MapPost("/movies", async (
            [FromServices] IKafkaProducer producer, 
            [FromBody] Movie movie) =>
        {
            await producer.ProduceAsync("movies", movie.Id, movie);
        });

        builder.MapStream<Guid, Movie>("movies")
            .SplitInto(x => {
                x.Branch((k, v) => v.Genre == Genre.Drama).To("drama-movies");
                x.Branch((k, v) => v.Genre == Genre.Horror).To("horror-movies");
                x.Branch((k, v) => v.Genre == Genre.Fantasy).To("fantasy-movies");
                x.DefaultBranch("unknown-movies");
            });
    }
}
