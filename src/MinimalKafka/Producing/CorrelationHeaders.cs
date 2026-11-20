namespace MinimalKafka.Producing;

internal static class CorrelationHeaders
{
    public const string CorrelationIdHeaderName = "x-correlation-id";

    public static string? ReadCorrelationId(KafkaHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return headers.Get(CorrelationIdHeaderName);
    }

    public static bool ContainsCorrelationId(KafkaHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return headers.Contains(CorrelationIdHeaderName);
    }

    public static void SetCorrelationId(KafkaHeaders headers, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Set(CorrelationIdHeaderName, correlationId);
    }
}
