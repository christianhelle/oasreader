using Microsoft.OpenApi;

namespace OasReader.Visitors;

internal class ReferenceCache
{
    private readonly Dictionary<ReferenceType, Dictionary<string, IOpenApiReferenceable>> data = new();

    public int Count => data.Sum(x => x.Value.Count);

    public void Add(ReferenceType type, string id, IOpenApiReferenceable referenceable)
    {
        if (!data.ContainsKey(type))
        {
            data[type] = new Dictionary<string, IOpenApiReferenceable>();
        }

        if (data[type].TryGetValue(id, out _))
        {
            return;
        }

        data[type][id] = referenceable;
    }

    public void UpdateDocument(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();

        foreach (var kvp in data)
        {
            switch (kvp.Key)
            {
                case ReferenceType.Schema:
                    document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                    Update(document.Components.Schemas, kvp.Value);
                    break;
                case ReferenceType.Response:
                    document.Components.Responses ??= new Dictionary<string, IOpenApiResponse>();
                    Update(document.Components.Responses, kvp.Value);
                    break;
                case ReferenceType.Parameter:
                    document.Components.Parameters ??= new Dictionary<string, IOpenApiParameter>();
                    Update(document.Components.Parameters, kvp.Value);
                    break;
                case ReferenceType.Example:
                    document.Components.Examples ??= new Dictionary<string, IOpenApiExample>();
                    Update(document.Components.Examples, kvp.Value);
                    break;
                case ReferenceType.RequestBody:
                    document.Components.RequestBodies ??= new Dictionary<string, IOpenApiRequestBody>();
                    Update(document.Components.RequestBodies, kvp.Value);
                    break;
                case ReferenceType.Header:
                    document.Components.Headers ??= new Dictionary<string, IOpenApiHeader>();
                    Update(document.Components.Headers, kvp.Value);
                    break;
                case ReferenceType.SecurityScheme:
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    Update(document.Components.SecuritySchemes, kvp.Value);
                    break;
                case ReferenceType.Link:
                    document.Components.Links ??= new Dictionary<string, IOpenApiLink>();
                    Update(document.Components.Links, kvp.Value);
                    break;
                case ReferenceType.Callback:
                    document.Components.Callbacks ??= new Dictionary<string, IOpenApiCallback>();
                    Update(document.Components.Callbacks, kvp.Value);
                    break;
                case ReferenceType.Tag:
                    document.Tags ??= new HashSet<OpenApiTag>();
                    Update(document.Tags, kvp.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kvp.Key));
            }
        }
    }

    private static void Update<T>(IDictionary<string, T> collection, Dictionary<string, IOpenApiReferenceable> data)
        where T : IOpenApiReferenceable
    {
        foreach (var kvp in data)
        {
            collection[kvp.Key] = (T)kvp.Value;
        }
    }

    private static void Update<T>(ICollection<T> collection, Dictionary<string, IOpenApiReferenceable> data)
        where T : IOpenApiReferenceable
    {
        foreach (var kvp in data)
        {
            collection.Add((T)kvp.Value);
        }
    }
}
