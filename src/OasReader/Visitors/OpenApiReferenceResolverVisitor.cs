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

            var id = reference.Id.Split('/').Last();
            var type = reference.Type;

            var resolved = ResolveFromDocument(externalDocument, type, id);
            if (resolved != null)
            {
                Cache.Add(type, id, resolved);
            }

            // Replace external reference with local reference
            referenceHolder.SetLocalReference(id, type);
        }

        private static IOpenApiReferenceable? ResolveFromDocument(OpenApiDocument document, ReferenceType type, string id)
        {
            if (document.Components == null)
                return null;

            return type switch
            {
                ReferenceType.Schema when document.Components.Schemas?.TryGetValue(id, out var schema) == true
                    => schema as IOpenApiReferenceable,
                ReferenceType.Response when document.Components.Responses?.TryGetValue(id, out var response) == true
                    => response as IOpenApiReferenceable,
                ReferenceType.Parameter when document.Components.Parameters?.TryGetValue(id, out var parameter) == true
                    => parameter as IOpenApiReferenceable,
                ReferenceType.Example when document.Components.Examples?.TryGetValue(id, out var example) == true
                    => example as IOpenApiReferenceable,
                ReferenceType.RequestBody when document.Components.RequestBodies?.TryGetValue(id, out var requestBody) == true
                    => requestBody as IOpenApiReferenceable,
                ReferenceType.Header when document.Components.Headers?.TryGetValue(id, out var header) == true
                    => header as IOpenApiReferenceable,
                ReferenceType.SecurityScheme when document.Components.SecuritySchemes?.TryGetValue(id, out var securityScheme) == true
                    => securityScheme as IOpenApiReferenceable,
                ReferenceType.Link when document.Components.Links?.TryGetValue(id, out var link) == true
                    => link as IOpenApiReferenceable,
                ReferenceType.Callback when document.Components.Callbacks?.TryGetValue(id, out var callback) == true
                    => callback as IOpenApiReferenceable,
                _ => null
            };
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
                var settings = new OpenApiReaderSettings();
                settings.AddYamlReader();
                var result = OpenApiDocument.Load(new MemoryStream(ReadAllBytes(stream)), settings: settings);
                return result.Document;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
