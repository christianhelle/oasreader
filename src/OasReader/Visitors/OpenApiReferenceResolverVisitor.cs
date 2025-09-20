using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace OasReader.Visitors
{
    internal class OpenApiReferenceResolverVisitor : OpenApiVisitorBase
    {
        private readonly Dictionary<string, OpenApiDocument> documentCache;
        private static readonly Lazy<HttpClient> HttpClient = new();
        private readonly List<FileInfo> files;
        private readonly string openApiFile;

        internal ReferenceCache Cache { get; } = new();

        public OpenApiReferenceResolverVisitor(
            string openApiFile,
            Dictionary<string, OpenApiDocument> documentCache)
        {
            this.documentCache = documentCache;
            this.openApiFile = openApiFile;

            if (!openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var directoryName = Path.GetDirectoryName(openApiFile);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    files = Directory
                        .GetFiles(directoryName, $"*{Path.GetExtension(openApiFile)}", SearchOption.AllDirectories)
                        .Select(f => new FileInfo(f))
                        .Where(f => f.Exists)
                        .ToList();
                }
                else
                {
                    files = new List<FileInfo>();
                }
            }
            else
            {
                files = new List<FileInfo>();
            }
        }

        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            if (!(referenceHolder.Reference?.IsExternal ?? false) ||
                !TryLoadDocument(referenceHolder, out var externalDocument) ||
                externalDocument == null)
            {
                return;
            }

            var localReference = new OpenApiReference
            {
                Id = referenceHolder.Reference!.Id.Split('/').Last(),
                Type = referenceHolder.Reference.Type ?? ReferenceType.Schema
            };

            if (externalDocument.ResolveReference(localReference) is { } reference)
            {
                Cache.Add(reference);
            }

            referenceHolder.Reference = localReference;
        }

        private bool TryLoadDocument(IOpenApiReferenceHolder referenceHolder, out OpenApiDocument? document)
        {
            document = null;
            var reference = referenceHolder.Reference?.IsExternal ?? false
                ? referenceHolder.Reference.ExternalResource
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
                try
                {
                    return GetDocumentFromStream(
                        reference,
                        HttpClient.Value.GetStreamAsync(new Uri(reference)).GetAwaiter().GetResult());
                }
                catch
                {
                    return null;
                }
            }

            if (openApiFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var baseUri = new Uri(openApiFile);
                    var absoluteUri = new Uri(baseUri, reference);
                    return GetDocumentFromStream(
                        absoluteUri.ToString(),
                        HttpClient.Value.GetStreamAsync(absoluteUri).GetAwaiter().GetResult());
                }
                catch
                {
                    return null;
                }
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