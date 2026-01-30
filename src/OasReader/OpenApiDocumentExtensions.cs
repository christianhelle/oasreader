using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Services;
using OasReader.Visitors;

namespace Microsoft.OpenApi.Models
{
    public static class OpenApiDocumentExtensions
    {
        public static string MergeExternalReferencesAsString(this OpenApiDocument document, string openApiFile)
        {
            document.MergeExternalReferences(openApiFile);

            return openApiFile.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
                   openApiFile.EndsWith("yml", StringComparison.OrdinalIgnoreCase)
                ? document.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0)
                : document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
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

            if (document.Components?.Schemas is not null)
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
                    kvp.Value.Parameters?
                        .Where(p => p is not null)
                        .Any(p =>
                            p.Reference?.IsExternal is true ||
                            p.Schema?.Reference?.IsExternal is true ||
                            p.Content?.Any(c => c.Value.Schema?.Reference?.IsExternal is true) is true) is true ||
                    kvp.Value.Operations?.Any(o =>
                        o.Value.Parameters?
                            .Where(p => p is not null)
                            .Any(p =>
                                p.Reference?.IsExternal is true ||
                                p.Schema?.Reference?.IsExternal is true ||
                                p.Content?.Any(c => c.Value.Schema?.Reference?.IsExternal is true) is true) is true ||
                        o.Value.RequestBody?.Content?.Any(c => c.Value.Schema?.Reference?.IsExternal is true) is true ||
                        o.Value.Responses?.Any(r =>
                            r.Value?.Content?.Any(c => c.Value?.Schema?.Reference?.IsExternal is true) is true ||
                            r.Value?.Headers?.Any(h => h.Value?.Schema?.Reference?.IsExternal is true) is true) is true) is true) is true;
    }
}