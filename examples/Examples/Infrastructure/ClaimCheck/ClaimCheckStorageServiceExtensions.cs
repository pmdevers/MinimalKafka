using Microsoft.Extensions.Configuration;
using MinimalKafka.Middleware.ClaimCheck;

namespace Examples.Infrastructure.ClaimCheck;

public static class ClaimCheckStorageServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClaimCheckStorage(IConfiguration configuration)
        {
            var section = configuration.GetSection(ClaimCheckStorageOptions.SectionName);

            services
                .AddOptions<ClaimCheckStorageOptions>()
                .Bind(section);

            var options = section.Get<ClaimCheckStorageOptions>() ?? new ClaimCheckStorageOptions();
            if (options.Provider == ClaimCheckStoreProvider.AzureBlob)
            {
                services.AddSingleton<IClaimCheckStore, AzureBlobClaimCheckStore>();
            }

            return services;
        }
    }
}
