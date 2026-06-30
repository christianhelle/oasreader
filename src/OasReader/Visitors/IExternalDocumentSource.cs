using Microsoft.OpenApi;

namespace OasReader.Visitors;

internal interface IExternalDocumentSource
{
    OpenApiDocument? GetDocument(string externalResource);
}
