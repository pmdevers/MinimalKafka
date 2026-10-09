namespace Examples.Infrastructure.ClaimCheck;

public enum ClaimCheckStoreProvider
{
    FileSystem,
    AzureBlob
}

public sealed class ClaimCheckStorageOptions
{
    public const string SectionName = "ClaimCheck:Storage";

    public ClaimCheckStoreProvider Provider { get; set; }

    public AzureBlobOptions AzureBlob { get; set; } = new();

    public sealed class AzureBlobOptions
    {
        public string? ConnectionString { get; set; }

        public string ContainerName { get; set; } = "claim-check";

        public string? BlobPrefix { get; set; }
    }
}
