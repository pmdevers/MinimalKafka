namespace FileTransfer.Configuration;

public static class OptionsConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddOptionsConfigs(ILogger logger)
        {
            var configuration = builder.Configuration;
            var services = builder.Services;

            // Configure ReplicatorOptions from configuration
            services.Configure<FileTransferOptions>(configuration.GetSection(FileTransferOptions.SectionName));

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.OptionsConfigured("Configuration");
            }

            return builder;
        }
    }
}
