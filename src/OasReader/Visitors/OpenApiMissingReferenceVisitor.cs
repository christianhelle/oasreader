using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Services;

namespace OasReader.Visitors
{
    internal class OpenApiMissingReferenceVisitor(
        OpenApiDocument document,
        Dictionary<string, OpenApiDocument> documentCache)
        : OpenApiVisitorBase
    {
        internal ReferenceCache Cache { get; } = new();

        public override void Visit(IOpenApiSchema schema)
        {
            if (schema is not OpenApiSchema openApiSchema ||
                document.Components.Schemas.ContainsKey(openApiSchema.Reference?.Id ?? ""))
            {
                return;
            }

            foreach (var kvp in documentCache)
            {
                try
                {
                    if (kvp.Value.ResolveReference(openApiSchema.Reference) is OpenApiSchema resolvedSchema)
                    {
                        Cache.Add(resolvedSchema);
                    }
                }
                catch
                {
                    // Ignored.
                    // When the reference cannot be found, an error is thrown.
                    // Do not log, but just continue searching...
                }
            }
        }
    }
}