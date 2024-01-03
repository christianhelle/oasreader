using System.Net;
using System.Security;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using OasReader.Merger;

namespace OasReader.OAS;

public static class OpenApiReader
{
    public static async Task<OpenApiDocument> Load(string openApiFile, CancellationToken cancellationToken)
    {
        var directoryName = new FileInfo(openApiFile).DirectoryName;
        var openApiReaderSettings = new OpenApiReaderSettings
        {
            BaseUrl = openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(openApiFile)
                : new Uri($"file://{directoryName}{Path.DirectorySeparatorChar}")
        };

        using var stream = await GetStream(openApiFile, cancellationToken);
        var streamReader = new OpenApiStreamReader(openApiReaderSettings);
        var result = await streamReader.ReadAsync(stream, cancellationToken);
        var document = result.OpenApiDocument;

        if (document.Paths.Any(pair => pair.Value.Parameters.Any(parameter => parameter.Reference?.IsExternal == true)))
        {
            var contents = OpenApiMerger.Merge(openApiFile, document, cancellationToken);
            var reader = new OpenApiStringReader();
            document = reader.Read(contents, out var diagnostic);
        }

        return document ?? throw new InvalidOperationException($"Could not read the OpenAPI file at {openApiFile}");
    }

    public static async Task<Stream> GetStream(
        string input,
        CancellationToken cancellationToken)
    {
        if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var httpClientHandler = new HttpClientHandler()
                {
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using var httpClient = new HttpClient(httpClientHandler);
                return await httpClient.GetStreamAsync(input);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {input}", ex);
            }
        }

        try
        {
            var fileInput = new FileInfo(input);
            return fileInput.OpenRead();
        }
        catch (Exception ex) when (ex is FileNotFoundException ||
                                   ex is PathTooLongException ||
                                   ex is DirectoryNotFoundException ||
                                   ex is IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is SecurityException ||
                                   ex is NotSupportedException)
        {
            throw new InvalidOperationException($"Could not open the file at {input}", ex);
        }
    }
}