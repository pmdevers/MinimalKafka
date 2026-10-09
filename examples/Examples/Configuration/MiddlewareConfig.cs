using Examples.Features.UI;
using FileTransfer.Features;
using Scalar.AspNetCore;

namespace Examples.Configuration;

public static class MiddlewareConfig
{
    extension(WebApplication app)
    {
        public WebApplication UseAppMiddleware()
        {
            // Configure the HTTP request pipeline.
            app.MapHealthChecks("/ready", new() { Predicate = _ => true });
            app.MapHealthChecks("/startup", new() { Predicate = _ => true });
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

            app.MapFeatures();

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