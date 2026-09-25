using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

    public async Task<KafkaFile> LoadData(KafkaFile kafkaFile)
    {
        await _containerClient.CreateIfNotExistsAsync();
        var blobClient = _containerClient.GetBlobClient(kafkaFile.Id.ToString("N"));

        if (!await blobClient.ExistsAsync())
        {
            return kafkaFile;
        }

        var content = await blobClient.DownloadContentAsync();

        return kafkaFile with
        {
            Data = content.Value.Content.ToMemory()
        };
    }

    public async Task StoreAsync(KafkaFile kafkaFile)
    {
        await _containerClient.CreateIfNotExistsAsync();
        var blobClient = _containerClient.GetBlobClient(kafkaFile.Id.ToString("N"));

        await using var stream = new MemoryStream(kafkaFile.Data.ToArray());
        await blobClient.UploadAsync(stream, overwrite: true);
        await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
        {
            ContentType = kafkaFile.ContentType
        });
    }
}
