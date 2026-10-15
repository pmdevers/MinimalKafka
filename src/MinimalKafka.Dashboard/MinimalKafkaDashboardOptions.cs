namespace MinimalKafka.Dashboard;

/// <summary>Configuration for the MinimalKafka dashboard endpoints and UI route.</summary>
public sealed class MinimalKafkaDashboardOptions
{
    /// <summary>Base route where the dashboard UI and APIs are exposed.</summary>
    public string RoutePrefix { get; set; } = "/kafka-dashboard";
}
