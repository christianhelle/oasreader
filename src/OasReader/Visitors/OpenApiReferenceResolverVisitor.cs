using Microsoft.OpenApi;

namespace OasReader.Visitors
{
    internal class OpenApiReferenceResolverVisitor : OpenApiVisitorBase
    {
        private readonly Dictionary<string, OpenApiDocument> documentCache;
        private readonly IExternalDocumentSource source;

        internal ReferenceCache Cache { get; } = new();

        public OpenApiReferenceResolverVisitor(
            IExternalDocumentSource source,
            Dictionary<string, OpenApiDocument> documentCache)
        {
            this.source = source;
            this.documentCache = documentCache;
        }

        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            var reference = referenceHolder.GetBaseReference();
            if (reference == null || !reference.IsExternal)
            {
                return;
            }

            if (!TryLoadDocument(reference, out var externalDocument) ||
                externalDocument == null)
            {
                return;
            }

            var id = reference.Id?.Split('/').Last();
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var type = reference.Type;

            var resolved = ComponentResolver.ResolveFromDocument(externalDocument, type, id!);
            if (resolved != null)
            {
                Cache.Add(type, id!, resolved);
                referenceHolder.SetLocalReference(id!, type);
            }
        }

        private bool TryLoadDocument(BaseOpenApiReference reference, out OpenApiDocument? document)
        {
            document = null;
            var externalResource = reference.IsExternal
                ? reference.ExternalResource
                : null;
            if (externalResource == null)
            {
                return false;
            }

            if (documentCache.TryGetValue(externalResource, out OpenApiDocument? value))
            {
                document = value;
                return true;
            }

            var externalDocument = source.GetDocument(externalResource);
            if (externalDocument == null)
            {
                return false;
            }

            documentCache[externalResource] = externalDocument;
            document = documentCache[externalResource];
            return true;
        }
    }
}
