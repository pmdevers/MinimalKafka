public static class InfrastructureExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddInfrastructure(ILogger logger)
        {
            var services = builder.Services;

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