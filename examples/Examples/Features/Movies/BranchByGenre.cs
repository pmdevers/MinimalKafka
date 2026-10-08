using Examples.Domain;
using MinimalKafka;
using MinimalKafka.Attributes;

namespace Examples.Features.Movies;

public static class BranchByGenre
{
    public const string MovieTopic = "movies";

    public static async Task Handle(KafkaContext context, [FromKey] Guid Key, [FromValue] Movie movie)
    {
        await context.Producer.ProduceAsync($"{movie.Genre}-{context.Topic}", movie.Id, movie);
    }
}
