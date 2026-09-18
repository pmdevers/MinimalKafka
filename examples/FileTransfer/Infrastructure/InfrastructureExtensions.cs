using FileTransfer.Infrastructure;

public static class InfrastructureExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddInfrastructure(ILogger logger)
        {
            var services = builder.Services;

            services.AddScoped<AzureBlobStorage>();

            if (builder.Environment.IsDevelopment())
            {

            }
            else
            {

            }

            logger.ServicesRegistered("Infrastructure");

            return builder;
        }
    }
}