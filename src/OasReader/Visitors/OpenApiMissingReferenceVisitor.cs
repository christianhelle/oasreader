using Microsoft.OpenApi;

namespace OasReader.Visitors
{
    internal class OpenApiMissingReferenceVisitor(
        OpenApiDocument document,
        Dictionary<string, OpenApiDocument> documentCache)
        : OpenApiVisitorBase
    {
        internal ReferenceCache Cache { get; } = new();

        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            var reference = referenceHolder.GetBaseReference();
            if (reference == null)
            {
                return;
            }

            var id = reference.Id?.Split('/').Last();
            var type = reference.Type;

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            // Check if already exists in the main document
            if (ComponentResolver.ExistsInDocument(document, type, id))
            {
                return;
            }

            // Search in cached external documents
            foreach (var kvp in documentCache)
            {
                try
                {
                    var resolved = ComponentResolver.ResolveFromDocument(kvp.Value, type, id);
                    if (resolved != null)
                    {
                        Cache.Add(type, id, resolved);
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
