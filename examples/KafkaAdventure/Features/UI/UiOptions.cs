using Microsoft.Extensions.FileProviders;

namespace KafkaAdventure.Features.UI;

public static class UiOptions
{
    public static IFileProvider CreateFileProvider()
    {
        var assembly = typeof(UiOptions).Assembly;
        return new ManifestEmbeddedFileProvider(assembly, "Features/UI");
    }
}
