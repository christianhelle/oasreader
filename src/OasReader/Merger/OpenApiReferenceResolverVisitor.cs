using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace OasReader.Merger.Visitors
{
    public class OpenApiReferenceResolverVisitor : OpenApiVisitorBase
    {
        private readonly Dictionary<string, OpenApiDocument> documentCache;
        private static readonly Lazy<HttpClient> HttpClient = new();
        private readonly List<FileInfo> files;
        
        internal ReferenceCache Cache { get; } = new();

        public OpenApiReferenceResolverVisitor(
            string input,
            Dictionary<string, OpenApiDocument> documentCache)
        {
            this.documentCache = documentCache;

            files = Directory
                .GetFiles(Path.GetDirectoryName(input)!, $"*{Path.GetExtension(input)}", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => f.Exists)
                .ToList();
        }

        public override void Visit(IOpenApiReferenceable referenceable)
        {
            if (!(referenceable.Reference?.IsExternal ?? false) ||
                !TryLoadDocument(referenceable, out var externalDocument) ||
                externalDocument == null)
            {
                return;
            }

            var localReference = new OpenApiReference
            {
                Id = referenceable.Reference!.Id.Split('/').Last(),
                Type = referenceable.Reference.Type ?? ReferenceType.Schema
            };

            if (externalDocument.ResolveReference(localReference) is { } reference)
            {
                Cache.Add(reference);
            }

            referenceable.Reference = localReference;
        }

        private bool TryLoadDocument(IOpenApiReferenceable referenceable, out OpenApiDocument? document)
        {
            document = null;
            var reference = referenceable.Reference?.IsExternal ?? false
                ? referenceable.Reference.ExternalResource
                : null;
            if (reference == null)
            {
                return false;
            }

            if (documentCache.TryGetValue(reference, out OpenApiDocument? value))
            {
                document = value;
                return true;
            }

            var externalDocument = GetDocument(reference);
            if (externalDocument == null)
            {
                return false;
            }

            documentCache[reference] = externalDocument;
            document = documentCache[reference];
            return true;
        }

        private OpenApiDocument? GetDocument(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            if (reference.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return GetDocumentFromStream(
                    reference,
                    HttpClient.Value.GetStreamAsync(new Uri(reference)).GetAwaiter().GetResult());
            }

            var file = files.FirstOrDefault(
                f => f.FullName.EndsWith(
                    reference
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar)));

            if (file == null)
            {
                return null;
            }

            using var fs = file.OpenRead();
            return GetDocumentFromStream(reference, fs);
        }

        private static OpenApiDocument? GetDocumentFromStream(string reference, Stream stream)
        {
            try
            {
                return new OpenApiStreamReader().Read(stream, out var results);
            }
            catch
            {
                return null;
            }
        }
    }
}