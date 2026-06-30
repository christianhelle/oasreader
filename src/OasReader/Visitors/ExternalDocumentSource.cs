using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OasReader.Visitors;

internal class ExternalDocumentSource : IExternalDocumentSource
{
    private static readonly Lazy<HttpClient> HttpClient = new();
    private readonly List<FileInfo> files;
    private readonly string openApiFile;

    public ExternalDocumentSource(string openApiFile)
    {
        this.openApiFile = openApiFile;

        if (!openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var directoryName = Path.GetDirectoryName(openApiFile);
            if (!string.IsNullOrEmpty(directoryName))
            {
                files = Directory
                    .GetFiles(directoryName, $"*{Path.GetExtension(openApiFile)}", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.Exists)
                    .ToList();
            }
            else
            {
                files = new List<FileInfo>();
            }
        }
        else
        {
            files = new List<FileInfo>();
        }
    }

    public OpenApiDocument? GetDocument(string externalResource)
    {
        if (string.IsNullOrWhiteSpace(externalResource))
        {
            return null;
        }

        if (externalResource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return GetDocumentFromStream(
                    HttpClient.Value.GetStreamAsync(new Uri(externalResource)).GetAwaiter().GetResult());
            }
            catch
            {
                return null;
            }
        }

        if (openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var baseUri = new Uri(openApiFile);
                var absoluteUri = new Uri(baseUri, externalResource);
                return GetDocumentFromStream(
                    HttpClient.Value.GetStreamAsync(absoluteUri).GetAwaiter().GetResult());
            }
            catch
            {
                return null;
            }
        }

        var file = files.FirstOrDefault(
            f => f.FullName.EndsWith(
                externalResource
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)));

        if (file == null)
        {
            return null;
        }

        using var fs = file.OpenRead();
        return GetDocumentFromStream(fs);
    }

    internal static OpenApiDocument? GetDocumentFromStream(Stream stream)
    {
        try
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            var settings = new OpenApiReaderSettings();
            settings.AddYamlReader();
            var result = OpenApiDocument.Load(ms, settings: settings);
            return result.Document;
        }
        catch
        {
            return null;
        }
    }
}
