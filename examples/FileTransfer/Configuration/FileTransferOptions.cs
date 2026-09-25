namespace FileTransfer.Configuration;

public class FileTransferOptions
{
    public const string SectionName = "FileTransfer";

    public string BlobStorageConnectionString { get; set; } = "UseDevelopmentStorage=true";

    public string BlobContainerName { get; set; } = "kafka-files";
}
