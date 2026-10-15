using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Contracts;
using MinimalKafka.Dashboard.Services;

namespace MinimalKafka.Dashboard;

/// <summary>Extension methods for registering and mapping the MinimalKafka dashboard.</summary>
public static class MinimalKafkaDashboardExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds services required by the MinimalKafka dashboard.</summary>
        /// <param name="configure">Optional dashboard configuration callback.</param>
        /// <returns>The same service collection for chaining.</returns>
        public IServiceCollection AddMinimalKafkaDashboard(Action<MinimalKafkaDashboardOptions>? configure = null)
        {
            services.AddOptions<MinimalKafkaDashboardOptions>();
            if (configure is not null)
            {
                services.Configure(configure);
            }

            services.TryAddSingleton<IKafkaDashboardQueryService, KafkaDashboardQueryService>();

            return services;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps the dashboard UI and its consumer-group API endpoints.</summary>
        /// <returns>The same endpoint route builder for chaining.</returns>
        public IEndpointRouteBuilder MapMinimalKafkaDashboard()
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<MinimalKafkaDashboardOptions>>().Value;
            var routePrefix = NormalizeRoutePrefix(options.RoutePrefix);

            var dashboard = endpoints.MapGroup(routePrefix);
            dashboard.WithTags("MinimalKafka Dashboard");

            dashboard.MapGet("/", () => Results.Content(DashboardUiContent.GetIndexHtml(), "text/html"));

            dashboard.MapGet("/api/consumer-group/members", async (IKafkaDashboardQueryService service, CancellationToken cancellationToken) =>
            {
                var response = await service.GetMembersAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            });

            dashboard.MapGet("/api/consumer-group/offsets", async (IKafkaDashboardQueryService service, CancellationToken cancellationToken) =>
            {
                var response = await service.GetOffsetsAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            });

            dashboard.MapPost("/api/consumer-group/offsets/reset", async (ConsumerGroupOffsetResetRequest request, IKafkaDashboardQueryService service, CancellationToken cancellationToken) =>
            {
                var response = await service.ResetOffsetsAsync(request, cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            });

            dashboard.MapGet("/api/topics/messages", async (string topic, int? count, IKafkaDashboardQueryService service, CancellationToken cancellationToken) =>
            {
                var response = await service.GetTopicMessagesAsync(topic, count ?? 50, cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            });

            dashboard.MapGet("/api/consumer/status", (IKafkaDashboardQueryService service) =>
                Results.Ok(service.GetConsumerStatus()));

            dashboard.MapPost("/api/consumer/start", (IKafkaDashboardQueryService service) =>
                Results.Ok(service.StartConsumers()));

            dashboard.MapPost("/api/consumer/stop", (IKafkaDashboardQueryService service) =>
                Results.Ok(service.StopConsumers()));

            return endpoints;
        }
    }

    private static string NormalizeRoutePrefix(string routePrefix)
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

        return normalized.TrimEnd('/');
    }
}
