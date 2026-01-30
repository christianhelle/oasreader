using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace OasReader.Visitors
{
    internal class OpenApiMissingReferenceVisitor(
        OpenApiDocument document,
        Dictionary<string, OpenApiDocument> documentCache)
        : OpenApiVisitorBase
    {
        internal ReferenceCache Cache { get; } = new();

        public override void Visit(IOpenApiReferenceable referenceable)
        {
            if (referenceable is not OpenApiSchema ||
                referenceable.Reference?.Id is null ||
                document.Components?.Schemas?.ContainsKey(referenceable.Reference.Id) is true)
            {
                return;
            }

            foreach (var kvp in documentCache)
            {
                try
                {
                    if (kvp.Value.ResolveReference(referenceable.Reference) is OpenApiSchema schema)
                    {
                        Cache.Add(schema);
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