using KafkaAdventure.Features;
using KafkaAdventure.Features.UI;
using Microsoft.AspNetCore.HttpOverrides;
using MinimalKafka.Dashboard;

namespace KafkaAdventure.Configuration;

public static class MiddlewareConfig
{
    extension(WebApplication app)
    {
        public WebApplication UseAppMiddleware()
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.MapHealthChecks("/startup");
            app.MapHealthChecks("/liveness");
            app.MapHealthChecks("/ready");

            var fileProvider = UiOptions.CreateFileProvider();

            

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });

            app.MapFeatures();
            app.MapMinimalKafkaDashboard();

            app.MapFallback(async context =>
            {
                var file = fileProvider.GetFileInfo("index.html");
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(file);
            });

            return app;
        }
    }
}
