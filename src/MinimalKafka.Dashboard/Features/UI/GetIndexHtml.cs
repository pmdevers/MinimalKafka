using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System.Text;

namespace MinimalKafka.Dashboard.Features.UI;

internal static class GetIndexHtml
{
    private static readonly Lazy<string> _indexHtml = new(LoadIndexHtml, LazyThreadSafetyMode.ExecutionAndPublication);

    internal const string Route = "/";

    internal static IResult Handle()
    {
        return Results.Content(_indexHtml.Value, "text/html");
    }

    private static string LoadIndexHtml()
    {
        var provider = new ManifestEmbeddedFileProvider(typeof(GetIndexHtml).Assembly, "Features/UI");
        var file = provider.GetFileInfo("index.html");
        if (!file.Exists)
        {
            throw new InvalidOperationException("Embedded dashboard UI resource 'FeaturesUI/index.html' was not found.");
        }

        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}