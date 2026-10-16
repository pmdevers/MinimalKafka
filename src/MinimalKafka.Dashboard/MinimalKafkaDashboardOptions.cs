namespace MinimalKafka.Dashboard;

/// <summary>Configuration for the MinimalKafka dashboard endpoints and UI route.</summary>
public sealed class MinimalKafkaDashboardOptions
{
    /// <summary>Base route where the dashboard UI and APIs are exposed.</summary>
    public string RoutePrefix { get; set; } = "/kafka-dashboard";

    /// <summary>Timeout for requests to the Kafka cluster.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    internal static string NormalizeRoutePrefix(string routePrefix)
    {
        if (string.IsNullOrWhiteSpace(routePrefix))
        {
            return "/minimal-kafka/dashboard";
        }

        var normalized = routePrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        var trimmed = normalized.TrimEnd('/');
        return trimmed.Length == 0 ? "/" : trimmed;
    }
}
