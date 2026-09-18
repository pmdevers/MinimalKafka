using FileTransfer.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka;

namespace FileTransfer.Features.Files;

public class UploadFile
{
    public static async Task<IResult> Handle(
        [FromServices] IKafkaProducer producer,
        IFormFileCollection files
    )
    {
        var list = new List<KafkaFile>();

        foreach (var file in files)
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);

            var kFile = KafkaFile.Create(file.FileName, file.ContentType, stream.ToArray());


            await producer.ProduceAsync("file-upload", Guid.NewGuid(), new
            {
                File = kFile
            });

        }

        return TypedResults.Ok(list);
    }

    public static async Task Consumer([FromValue] FileUpload fileUpload)
    {

    }

    public record FileUpload(KafkaFile File);

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

