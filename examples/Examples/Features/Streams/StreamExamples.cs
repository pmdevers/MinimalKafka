using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;
using MinimalKafka.Stream;
using System.Collections.Concurrent;

namespace Examples.Features.Streams;

public static class StreamExamples
{
    private static readonly ConcurrentQueue<OrderPaymentSummary> Results = [];

    public const string OrdersTopic = "stream-orders";
    public const string PaymentsTopic = "stream-payments";
    public const string ResultsTopic = "stream-results";

    public static async Task<IResult> CreateOrderAsync(
        [FromServices] IKafkaProducer producer,
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new OrderReceived(Guid.NewGuid(), request.CustomerName, request.Amount);
        await producer.ProduceAsync(OrdersTopic, order.Id, order, cancellationToken: cancellationToken);
        return TypedResults.Accepted($"/stream/results/{order.Id}", order);
    }

    public static async Task<IResult> CreatePaymentAsync(
        [FromServices] IKafkaProducer producer,
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = new PaymentReceived(request.OrderId, request.Amount, request.Reference);
        await producer.ProduceAsync(PaymentsTopic, payment.OrderId, payment, cancellationToken: cancellationToken);
        return TypedResults.Accepted($"/stream/results/{payment.OrderId}", payment);
    }

    public static IResult GetResults()
    {
        return TypedResults.Ok(Results.Reverse().ToArray());
    }

    public static IResult ClearResults()
    {
        while (Results.TryDequeue(out _))
        {
        }

        return TypedResults.NoContent();
    }

    public static async Task ProcessJoinAsync(
        StreamContext context,
        Guid orderId,
        (OrderReceived? Order, PaymentReceived? Payment) joined)
    {
        if (joined.Order is null || joined.Payment is null)
        {
            return;
        }

        var summary = new OrderPaymentSummary(
            orderId,
            joined.Order.CustomerName,
            joined.Order.Amount,
            joined.Payment.Amount,
            joined.Payment.Reference,
            DateTimeOffset.UtcNow);

        await context.ProduceAsync(ResultsTopic, orderId, summary);
    }

    public static Task TrackResultAsync(StreamContext context, Guid orderId, OrderPaymentSummary summary)
    {
        _ = context;
        _ = orderId;

        Results.Enqueue(summary);
        while (Results.Count > 50 && Results.TryDequeue(out _))
        {
        }

        return Task.CompletedTask;
    }

    public sealed record CreateOrderRequest(string CustomerName, decimal Amount);

    public sealed record CreatePaymentRequest(Guid OrderId, decimal Amount, string Reference);

    public sealed record OrderReceived(Guid Id, string CustomerName, decimal Amount);

    public sealed record PaymentReceived(Guid OrderId, decimal Amount, string Reference);

    public sealed record OrderPaymentSummary(
        Guid OrderId,
        string CustomerName,
        decimal OrderAmount,
        decimal PaymentAmount,
        string Reference,
        DateTimeOffset JoinedAtUtc);
}
