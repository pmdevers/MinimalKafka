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

            app.MapFeatures();

            return app;
        }
    }
}