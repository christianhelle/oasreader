using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

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

            var externalDocument = GetDocument(externalResource);
            if (externalDocument == null)
            {
                return false;
            }

            documentCache[externalResource] = externalDocument;
            document = documentCache[externalResource];
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
            return GetDocumentFromStream(fs);
        }

        private static OpenApiDocument? GetDocumentFromStream(Stream stream)
        {
            try
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                var settings = new OpenApiReaderSettings();
                settings.AddYamlReader();
                var result = OpenApiDocument.Load(ms, settings: settings);
                return result.Document;
            }
            catch
            {
                return null;
            }
        }
    }
}
