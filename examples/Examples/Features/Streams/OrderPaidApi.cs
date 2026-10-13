using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.Streams;

public static class OrderPaidApi
{
    public static async Task<IResult> CreateAsync(
        [FromServices] IKafkaProducer producer,
        [FromBody] CreateOrderPaidRequest request,
        CancellationToken cancellationToken)
    {
        var payment = new OrderPaid(request.OrderId, request.Amount, request.Reference);
        await producer.ProduceAsync(StreamTopics.Payments, payment.OrderId, payment, cancellationToken: cancellationToken);
        return TypedResults.Accepted($"/stream/results/{payment.OrderId}", payment);
    }

    public sealed record CreateOrderPaidRequest(Guid OrderId, decimal Amount, string Reference);
}
