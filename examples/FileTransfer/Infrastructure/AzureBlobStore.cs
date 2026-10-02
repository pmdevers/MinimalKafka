using Azure.Storage.Blobs;
using FileTransfer.Configuration;
using Microsoft.Extensions.Options;
using MinimalKafka;

namespace FileTransfer.Infrastructure;

public static class MinimalKafkaBlobStoreExtensions
{
    extension(IKafkaConfigBuilder builder)
    {
        public IKafkaConfigBuilder WithAzureBlobFileStore()
        {
            return builder.WithFileStore(x => x.GetRequiredService<AzureBlobStorage>());
        }
    }
}


public class AzureBlobStorage(IOptions<FileTransferOptions> options) : IKafkaFileStore
{
    private readonly BlobContainerClient _containerClient = new(options.Value.BlobStorageConnectionString, options.Value.BlobContainerName);

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

    public async Task StoreAsync(Guid key, ReadOnlyMemory<byte> data)
    {
        await _containerClient.CreateIfNotExistsAsync();
        var blobClient = _containerClient.GetBlobClient(key.ToString("N"));

        await using var stream = new MemoryStream(data.ToArray());
        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
