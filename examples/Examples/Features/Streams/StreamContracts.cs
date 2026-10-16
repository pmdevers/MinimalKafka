namespace Examples.Features.Streams;

public static class StreamTopics
{
    public const string Orders = "orders";
    public const string Payments = "payments";
    public const string PayedOrders = "payed-orders";
    public const string LowRiskOrders = "low-risk-orders";
    public const string MediumRiskOrders = "medium-risk-orders";
    public const string HighRiskOrders = "high-risk-orders";
}

public sealed record OrderPlaced(Guid Id, string CustomerName, decimal Amount);

public sealed record OrderPaid(Guid OrderId, decimal Amount, string Reference);

public sealed record PayedOrder(
    Guid OrderId,
    string CustomerName,
    decimal OrderAmount,
    decimal PaymentAmount,
    string Reference,
    DateTimeOffset PaidAtUtc);

public sealed record RiskDecision(
    Guid OrderId,
    string CustomerName,
    decimal Amount,
    string Branch,
    DateTimeOffset RoutedAtUtc);
