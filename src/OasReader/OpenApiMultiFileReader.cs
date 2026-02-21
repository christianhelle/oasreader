using System.Net;
using System.Security;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Microsoft.OpenApi.Reader;

public static class OpenApiMultiFileReader
{
    public static async Task<Result> Read(
        string openApiFile,
        ValidationRuleSet? validationRuleSet = default,
        CancellationToken cancellationToken = default)
    {
        var directoryName = new FileInfo(openApiFile).DirectoryName;
        var openApiReaderSettings = new OpenApiReaderSettings
        {
            BaseUrl = openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(openApiFile)
                : new Uri($"file://{directoryName}{Path.DirectorySeparatorChar}"),
        };

        if (validationRuleSet != null)
        {
            openApiReaderSettings.RuleSet = validationRuleSet;
        }
        else
        {
            openApiReaderSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();
        }

        openApiReaderSettings.AddYamlReader();

        using var stream = await GetStream(openApiFile);
        var result = await OpenApiDocument.LoadAsync(stream, settings: openApiReaderSettings, cancellationToken: cancellationToken);
        var document = result.Document;

        bool containedExternalReferences = false;
        if (result.Document.ContainsExternalReferences())
        {
            containedExternalReferences = true;
            document = document.MergeExternalReferences(openApiFile);
        }

        return new Result(result.Diagnostic, document, containedExternalReferences);
    }

    private static async Task<Stream> GetStream(string input)
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

public class Result
{
    public Result(
        OpenApiDiagnostic openApiDiagnostic,
        OpenApiDocument openApiDocument,
        bool containedExternalReferences)
    {
        OpenApiDiagnostic = openApiDiagnostic;
        OpenApiDocument = openApiDocument;
        ContainedExternalReferences = containedExternalReferences;
    }

    public OpenApiDiagnostic OpenApiDiagnostic { get; }
    public OpenApiDocument OpenApiDocument { get; }
    public bool ContainedExternalReferences { get; }
}
