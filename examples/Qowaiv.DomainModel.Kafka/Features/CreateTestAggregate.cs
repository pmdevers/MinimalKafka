using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Qowaiv.DomainModel.Kafka.Domain;

namespace Qowaiv.DomainModel.Kafka.Features;

public static class CreateTestAggregate
{
    public static void MapCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder, IEndpointRouteBuilder 
    {
        builder.MapGet("/create", Create);
        builder.MapGet("/changename/{id}/{name}/{description}", ChangeName);
        builder.MapGet("/{id}", GetById);
    }

    private static async Task<Accepted<TestId>> ChangeName(
        [FromServices] IAggregateStore<TestId, TestAggregate> store,
        [FromRoute(Name = "id")] TestId id,
        [FromRoute(Name = "name")] string name,
        [FromRoute(Name = "description")] string description)
    {
        var aggregate = await store.LoadAsync(id);

        var result = aggregate.ChangeName(name, description);

        if (result.IsValid)
        {
            await store.SaveAsync(result.Value);
        }

        return TypedResults.Accepted("/{id}", id);
    }

    private static async Task<TestAggregate> GetById(
        [FromServices] IAggregateStore<TestId, TestAggregate> store,
        [FromRoute(Name = "id")] TestId id
        )
    {
        var aggregate = await store.LoadAsync(id);
        return aggregate;
    }

    private static async Task<Accepted<TestId>> Create(
        [FromServices] IAggregateStore<TestId, TestAggregate> store
        )
    {
        var id = TestId.Next();
        var result = TestAggregate.Create(id);

        if (result.IsValid)
        {
            await store.SaveAsync(result.Value);
        }

        return TypedResults.Accepted("/get/{id}", id);
    }
}
