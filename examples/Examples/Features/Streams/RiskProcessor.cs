using MinimalKafka.Stream;
using System.Collections.Concurrent;

namespace Examples.Features.Streams;

public static class RiskProcessor
{
    private static readonly ConcurrentQueue<RiskDecision> _riskResults = [];

    private const decimal _highRiskThreshold = 1000m;
    private const decimal _mediumRiskThreshold = 100m;
    private const int _maxStoredResults = 50;

    public static IResult GetResults()
    {
        return TypedResults.Ok(_riskResults.Reverse().ToArray());
    }

    public static IResult ClearResults()
    {
        while (_riskResults.TryDequeue(out _))
        {
        }

        return TypedResults.NoContent();
    }

    public static void ConfigureBranches(StreamBranchBuilder<Guid, PayedOrder> branches)
    {
        branches.Branch((_, order) => order.OrderAmount >= _highRiskThreshold, (context, orderId, order) =>
            RouteByRiskAsync(context, orderId, order, "high", StreamTopics.HighRiskOrders));

        branches.Branch((_, order) => order.OrderAmount >= _mediumRiskThreshold, (context, orderId, order) =>
            RouteByRiskAsync(context, orderId, order, "medium", StreamTopics.MediumRiskOrders));

        branches.DefaultBranch((context, orderId, order) =>
            RouteByRiskAsync(context, orderId, order, "low", StreamTopics.LowRiskOrders));
    }

    private static async Task RouteByRiskAsync(
        StreamContext context,
        Guid orderId,
        PayedOrder order,
        string branch,
        string topic)
    {
        _riskResults.Enqueue(new RiskDecision(orderId, order.CustomerName, order.OrderAmount, branch, DateTimeOffset.UtcNow));
        while (_riskResults.Count > _maxStoredResults && _riskResults.TryDequeue(out _))
        {
        }

        await context.ProduceAsync(topic, orderId, order);
    }
}
