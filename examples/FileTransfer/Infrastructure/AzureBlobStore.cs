using Azure.Storage.Blobs;
using FileTransfer.Configuration;
using Microsoft.Extensions.Options;
using MinimalKafka;

namespace FileTransfer.Infrastructure;

/// <summary>
/// Provides configuration extensions for registering Azure Blob Storage as the Kafka file store.
/// </summary>
public static class MinimalKafkaBlobStoreExtensions
{
    extension(IKafkaConfigBuilder builder)
    {
        /// <summary>
        /// Configures MinimalKafka to persist file payload data in Azure Blob Storage.
        /// </summary>
        /// <returns>The configured Kafka builder.</returns>
        public IKafkaConfigBuilder WithAzureBlobFileStore()
        {
            return builder.WithFileStore(x => x.GetRequiredService<AzureBlobStorage>());
        }
    }
}

/// <summary>
/// Stores Kafka file payload data in an Azure Blob container.
/// </summary>
public class AzureBlobStorage(IOptions<FileTransferOptions> options) : IKafkaFileStore
{
    private readonly BlobContainerClient _containerClient = new(options.Value.BlobStorageConnectionString, options.Value.BlobContainerName);

    /// <summary>
    /// Loads file payload data from Azure Blob Storage for the specified file identifier.
    /// </summary>
    /// <param name="key">The unique file identifier.</param>
    /// <returns>The stored file payload, or an empty buffer when the blob does not exist.</returns>
    public async Task<ReadOnlyMemory<byte>> LoadAsync(Guid key)
    {
        await _containerClient.CreateIfNotExistsAsync();
        var blobClient = _containerClient.GetBlobClient(key.ToString("N"));

        if (!await blobClient.ExistsAsync())
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var content = await blobClient.DownloadContentAsync();
        return content.Value.Content.ToMemory();
    }

    /// <summary>
    /// Stores file payload data in Azure Blob Storage using the specified file identifier.
    /// </summary>
    /// <param name="key">The unique file identifier.</param>
    /// <param name="data">The file payload data to store.</param>
    public async Task StoreAsync(Guid key, ReadOnlyMemory<byte> data)
    {
        await _containerClient.CreateIfNotExistsAsync();
        var blobClient = _containerClient.GetBlobClient(key.ToString("N"));

        await using var stream = new MemoryStream(data.ToArray());
        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
