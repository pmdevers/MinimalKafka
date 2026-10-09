using Examples.Infrastructure.ClaimCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware.ClaimCheck;

namespace MinimalKafka.Tests;

public class ClaimCheckStorageRegistrationTests
{
    [Fact]
    public void AddClaimCheckStorage_ShouldKeepFileSystemStoreAsDefault()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration([
            new KeyValuePair<string, string?>("ClaimCheck:Storage:Provider", ClaimCheckStoreProvider.FileSystem.ToString())
        ]);

        services.AddClaimCheckStorage(configuration);
        services.AddMinimalKafka(builder => builder.WithClaimCheck());

        using var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetRequiredService<IClaimCheckStore>();

        store.Should().BeOfType<FileSystemClaimCheckStore>();
    }

    [Fact]
    public void AddClaimCheckStorage_ShouldUseAzureBlobStoreWhenConfigured()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration([
            new KeyValuePair<string, string?>("ClaimCheck:Storage:Provider", ClaimCheckStoreProvider.AzureBlob.ToString()),
            new KeyValuePair<string, string?>("ClaimCheck:Storage:AzureBlob:ConnectionString", "UseDevelopmentStorage=true"),
            new KeyValuePair<string, string?>("ClaimCheck:Storage:AzureBlob:ContainerName", "minimal-kafka-claim-check"),
            new KeyValuePair<string, string?>("ClaimCheck:Storage:AzureBlob:BlobPrefix", "tests")
        ]);

        services.AddClaimCheckStorage(configuration);
        services.AddMinimalKafka(builder => builder.WithClaimCheck());

        using var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetRequiredService<IClaimCheckStore>();

        store.Should().BeOfType<AzureBlobClaimCheckStore>();
    }

    private static IConfiguration BuildConfiguration(IEnumerable<KeyValuePair<string, string?>> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
