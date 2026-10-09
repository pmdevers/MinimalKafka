using Microsoft.Extensions.FileProviders;

namespace Examples.Features.UI;

public class UiOptions
{
    public static IFileProvider CreateFileProvider()
    {
        var assembly = typeof(UiOptions).Assembly;
        return new ManifestEmbeddedFileProvider(assembly, "Features/UI");
    }
}
