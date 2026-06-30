using Microsoft.OpenApi;
using OasReader.Visitors;

namespace OasReader.Tests;

internal sealed class InMemoryExternalDocumentSource : IExternalDocumentSource
{
    private readonly Dictionary<string, OpenApiDocument> documents;

    public InMemoryExternalDocumentSource(Dictionary<string, OpenApiDocument> documents) =>
        this.documents = documents;

    public int CallCount { get; private set; }

    public OpenApiDocument? GetDocument(string externalResource)
    {
        CallCount++;
        return documents.TryGetValue(externalResource, out var document)
            ? document
            : null;
    }
}
