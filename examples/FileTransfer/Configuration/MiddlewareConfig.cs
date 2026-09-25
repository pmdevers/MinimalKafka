using FileTransfer.Features;
using Scalar.AspNetCore;

namespace FileTransfer.Configuration;

public static class MiddlewareConfig
{
    extension(WebApplication app)
    {
        public WebApplication UseAppMiddleware()
        {
            // Configure the HTTP request pipeline.
            app.MapHealthChecks("/ready", new() { Predicate = check => check.Name != "replicator_health" });
            app.MapHealthChecks("/startup", new() { Predicate = check => check.Name != "replicator_health" });
            app.MapHealthChecks("/liveness", new() { Predicate = _ => true });

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference("/openapi");
            }

            app.UseAntiforgery();

            var fileProvider = UiOptions.CreateFileProvider();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });

            app.MapApiEndpoints();

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