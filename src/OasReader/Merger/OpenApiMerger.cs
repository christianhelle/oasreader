using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using OasReader.Merger.Visitors;


namespace OasReader.Merger
{
    public static class OpenApiMerger
    {
        public static string Merge(string input, OpenApiDocument document, CancellationToken cancellationToken)
        {
            var cache = new Dictionary<string, OpenApiDocument>();

            int missingCount;
            do
            {
                var referenceVisitor = new OpenApiReferenceResolverVisitor(input, cache);
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
            
            document.Components.Schemas = document.Components.Schemas
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return input.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
                   input.EndsWith("yml", StringComparison.OrdinalIgnoreCase)
                ? document.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0)
                : document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        }
    }
}