using OasReader.Visitors;

namespace Microsoft.OpenApi
{
    public static class OpenApiDocumentExtensions
    {
        public static async Task<string> MergeExternalReferencesAsStringAsync(this OpenApiDocument document, string openApiFile)
        {
            document.MergeExternalReferences(openApiFile);

            return openApiFile.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
                   openApiFile.EndsWith("yml", StringComparison.OrdinalIgnoreCase)
                ? await document.SerializeAsYamlAsync(OpenApiSpecVersion.OpenApi3_0)
                : await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
        }

        public static OpenApiDocument MergeExternalReferences(this OpenApiDocument document, string openApiFile)
        {
            var cache = new Dictionary<string, OpenApiDocument>();
            int missingCount;
            do
            {
                var referenceVisitor = new OpenApiReferenceResolverVisitor(openApiFile, cache);
                var referenceWalker = new OpenApiWalker(referenceVisitor);
                referenceWalker.Walk(document);
                referenceVisitor.Cache.UpdateDocument(document);

                var missingVisitor = new OpenApiMissingReferenceVisitor(document, cache);
                var missingWalker = new OpenApiWalker(missingVisitor);
                missingWalker.Walk(document);
                missingCount = missingVisitor.Cache.Count;
                missingVisitor.Cache.UpdateDocument(document);
            }
            while (missingCount > 0);

            document.Components ??= new OpenApiComponents();
            if (document.Components.Schemas != null)
            {
                document.Components.Schemas = document.Components.Schemas
                    .OrderBy(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return document;
        }

        public static bool ContainsExternalReferences(this OpenApiDocument document) =>
            document.Paths
                ?.Any(kvp =>
                    (kvp.Value.Parameters?
                        .Where(p => p is not null)
                        .Any(p =>
                            p.HasExternalReference() ||
                            p.Schema.HasExternalReference() ||
                            (p.Content?.Any(c => c.Value.Schema.HasExternalReference()) == true)) == true) ||
                    kvp.Value.Operations?.Any(o =>
                        (o.Value.Parameters?
                            .Where(p => p is not null)
                            .Any(p =>
                                p.HasExternalReference() ||
                                p.Schema.HasExternalReference() ||
                                p.Content?.Any(c => c.Value.Schema.HasExternalReference()) == true) == true) ||
                        o.Value.RequestBody?.Content?.Any(c => c.Value.Schema.HasExternalReference()) == true ||
                        o.Value.Responses?.Any(r =>
                            r.Value?.Content?.Any(c => c.Value?.Schema.HasExternalReference() == true) == true ||
                            r.Value?.Headers?.Any(h => h.Value?.Schema.HasExternalReference() == true) == true) == true) is true) is true;
    }

    internal static class OpenApiReferenceExtensions
    {
        internal static BaseOpenApiReference? GetBaseReference(this IOpenApiReferenceHolder holder)
        {
            return holder.GetType().GetProperty("Reference")?.GetValue(holder) as BaseOpenApiReference;
        }

        internal static bool HasExternalReference(this object? element)
        {
            if (element is not IOpenApiReferenceHolder holder) return false;
            return holder.GetBaseReference()?.IsExternal == true;
        }

        internal static void SetLocalReference(this IOpenApiReferenceHolder holder, string id, ReferenceType type)
        {
            var baseRef = holder.GetBaseReference();
            if (baseRef == null)
            {
                return;
            }

            // BaseOpenApiReference properties are init-only, so we must use reflection to set them
            var baseRefType = typeof(BaseOpenApiReference);
            baseRefType.GetProperty(nameof(BaseOpenApiReference.Id))?.SetValue(baseRef, id);
            baseRefType.GetProperty(nameof(BaseOpenApiReference.Type))?.SetValue(baseRef, type);
            baseRefType.GetProperty(nameof(BaseOpenApiReference.ExternalResource))?.SetValue(baseRef, null);
        }
    }
}
