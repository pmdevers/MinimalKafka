using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Domain;
using MinimalKafka.Dashboard.Features;
using MinimalKafka.Dashboard.Features.UI;
using MinimalKafka.Dashboard.Infrastructure;

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
            services.AddOptions<MinimalKafkaDashboardOptions>()
                .Validate(options => !string.IsNullOrWhiteSpace(options.RoutePrefix), "RoutePrefix cannot be empty.")
                .Validate(options => options.RequestTimeout > TimeSpan.Zero, "RequestTimeout must be greater than zero.");

            if (configure is not null)
            {
                services.Configure(configure);
            }

            services.PostConfigure<MinimalKafkaDashboardOptions>(options =>
            {
                options.RoutePrefix = MinimalKafkaDashboardOptions.NormalizeRoutePrefix(options.RoutePrefix);
            });

            services.AddOptions<MinimalKafkaDashboardOptions>()
                .Validate(options => options.RoutePrefix.StartsWith('/'), "RoutePrefix must start with '/'.");

            services.TryAddScoped<IKafkaDashboard, KafkaDashboard>();

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

            var dashboard = endpoints.MapGroup(options.RoutePrefix);
            dashboard.WithTags("MinimalKafka Dashboard");

            dashboard.MapGet(GetIndexHtml.Route, GetIndexHtml.Handle);

            dashboard.MapGet(GetMembers.Route, GetMembers.HandleAsync);
            dashboard.MapGet(GetOffsets.Route, GetOffsets.HandleAsync);
            dashboard.MapPost(ResetOffsets.Route, ResetOffsets.HandleAsync);

            dashboard.MapGet(GetTopicMessages.Route, GetTopicMessages.HandleAsync);

            dashboard.MapGet(GetConsumerStatus.Route, GetConsumerStatus.HandleAsync);
            dashboard.MapPost(StartConsumers.Route, StartConsumers.HandleAsync);
            dashboard.MapPost(StopConsumers.Route, StopConsumers.HandleAsync);

            return endpoints;
        }
    }
}
