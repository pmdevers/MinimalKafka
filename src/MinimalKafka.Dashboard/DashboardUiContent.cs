using Microsoft.Extensions.FileProviders;
using System.Text;

namespace MinimalKafka.Dashboard;

internal static class DashboardUiContent
{
    private static readonly Lazy<string> _indexHtml = new(LoadIndexHtml, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string GetIndexHtml() => _indexHtml.Value;

    private static string LoadIndexHtml()
    {
        var provider = new ManifestEmbeddedFileProvider(typeof(DashboardUiContent).Assembly, "UI");
        var file = provider.GetFileInfo("index.html");
        if (!file.Exists)
        {
            throw new InvalidOperationException("Embedded dashboard UI resource 'UI/index.html' was not found.");
        }

        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}
