using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.Streams;

public static class OrderPlacedApi
{
    public static async Task<IResult> CreateAsync(
        [FromServices] IKafkaProducer producer,
        [FromBody] CreateOrderPlacedRequest request,
        CancellationToken cancellationToken)
    {
        var order = new OrderPlaced(Guid.NewGuid(), request.CustomerName, request.Amount);
        await producer.ProduceAsync(StreamTopics.Orders, order.Id, order, cancellationToken: cancellationToken);
        return TypedResults.Accepted($"/stream/results/{order.Id}", order);
    }

    public sealed record CreateOrderPlacedRequest(string CustomerName, decimal Amount);
}
