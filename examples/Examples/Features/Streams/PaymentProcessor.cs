using MinimalKafka.Stream;
using System.Collections.Concurrent;

namespace Examples.Features.Streams;

public static class PaymentProcessor
{
    private static readonly ConcurrentQueue<PayedOrder> _results = [];
    private const int _maxStoredResults = 50;

    public static IResult GetResults()
    {
        return TypedResults.Ok(_results.Reverse().ToArray());
    }

    public static IResult ClearResults()
    {
        while (_results.TryDequeue(out _))
        {
        }

        return TypedResults.NoContent();
    }

    public static async Task ProcessJoinAsync(
        StreamContext context,
        Guid orderId,
        (OrderPlaced? Order, OrderPaid? Payment) joined)
    {
        if (joined.Order is null || joined.Payment is null)
        {
            return;
        }

        var payedOrder = new PayedOrder(
            orderId,
            joined.Order.CustomerName,
            joined.Order.Amount,
            joined.Payment.Amount,
            joined.Payment.Reference,
            DateTimeOffset.UtcNow);

        await context.ProduceAsync(StreamTopics.PayedOrders, orderId, payedOrder);
    }

    public static Task TrackPayedOrderAsync(StreamContext context, Guid orderId, PayedOrder payedOrder)
    {
        _ = context;
        _ = orderId;

        _results.Enqueue(payedOrder);
        while (_results.Count > _maxStoredResults && _results.TryDequeue(out _))
        {
        }

        return Task.CompletedTask;
    }
}
