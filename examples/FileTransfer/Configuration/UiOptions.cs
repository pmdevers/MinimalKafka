using Microsoft.Extensions.FileProviders;

namespace FileTransfer.Configuration;

public class UiOptions
{
    public static IFileProvider CreateFileProvider()
    {
        var assembly = typeof(UiOptions).Assembly;
        return new ManifestEmbeddedFileProvider(assembly, "Features/UI");
    }
}
