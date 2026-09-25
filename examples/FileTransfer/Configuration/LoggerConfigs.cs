using Serilog;

namespace FileTransfer.Configuration;

public static class LoggerConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddLoggerConfigs()
        {
            var configuration = builder.Configuration;

            // Configure logging
            builder.Logging.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .WriteTo.Console()
                .CreateLogger());

            return builder;
        }
    }
}
