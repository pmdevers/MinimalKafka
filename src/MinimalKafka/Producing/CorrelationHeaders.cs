using Confluent.Kafka;
using System.Text;

namespace MinimalKafka.Producing;

internal static class CorrelationHeaders
{
    public const string CorrelationIdHeaderName = "x-correlation-id";

    public static string? ReadCorrelationId(Headers headers)
    {
        var bytes = headers.LastOrDefault(static item => item.Key == CorrelationIdHeaderName)?.GetValueBytes();
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public static bool ContainsCorrelationId(Headers headers) =>
        headers.Any(static item => item.Key == CorrelationIdHeaderName);

    public static void SetCorrelationId(Headers headers, string correlationId)
    {
        headers.Remove(CorrelationIdHeaderName);
        headers.Add(CorrelationIdHeaderName, Encoding.UTF8.GetBytes(correlationId));
    }
}
