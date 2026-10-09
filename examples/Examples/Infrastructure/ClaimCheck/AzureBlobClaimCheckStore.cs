using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using MinimalKafka.Middleware.ClaimCheck;

namespace Examples.Infrastructure.ClaimCheck;

public sealed class AzureBlobClaimCheckStore(IOptions<ClaimCheckStorageOptions> options) : IClaimCheckStore
{
    private readonly BlobContainerClient _container = CreateContainerClient(options.Value.AzureBlob);
    private readonly string? _blobPrefix = NormalizePrefix(options.Value.AzureBlob.BlobPrefix);

    public async Task<string> StoreAsync(byte[] payload, CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid().ToString("N");

        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await BlobFor(claimId)
            .UploadAsync(BinaryData.FromBytes(payload), overwrite: false, cancellationToken)
            .ConfigureAwait(false);

        return claimId;
    }

    public async Task<byte[]?> RetrieveAsync(string claimId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await BlobFor(claimId)
                .DownloadContentAsync(cancellationToken)
                .ConfigureAwait(false);

            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string claimId, CancellationToken cancellationToken)
    {
        await BlobFor(claimId)
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private BlobClient BlobFor(string claimId)
    {
        if (!Guid.TryParseExact(claimId, "N", out var guid))
        {
            throw new ArgumentException("Invalid claim identifier.", nameof(claimId));
        }

        return _container.GetBlobClient($"{_blobPrefix}{guid:N}.claim");
    }

    private static BlobContainerClient CreateContainerClient(ClaimCheckStorageOptions.AzureBlobOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("ClaimCheck:Storage:AzureBlob:ConnectionString must be configured for Azure Blob claim-check storage.");
        }

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("ClaimCheck:Storage:AzureBlob:ContainerName must be configured for Azure Blob claim-check storage.");
        }

        return new BlobContainerClient(options.ConnectionString, options.ContainerName);
    }

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        return prefix.Trim('/').Length == 0
            ? null
            : prefix.Trim('/') + "/";
    }
}
