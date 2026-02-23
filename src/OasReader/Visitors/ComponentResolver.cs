using Microsoft.OpenApi;

namespace OasReader.Visitors;

internal static class ComponentResolver
{
    internal static bool ExistsInDocument(OpenApiDocument document, ReferenceType type, string id)
    {
        if (document.Components == null) return false;

        return type switch
        {
            ReferenceType.Schema => document.Components.Schemas?.ContainsKey(id) == true,
            ReferenceType.Response => document.Components.Responses?.ContainsKey(id) == true,
            ReferenceType.Parameter => document.Components.Parameters?.ContainsKey(id) == true,
            ReferenceType.Example => document.Components.Examples?.ContainsKey(id) == true,
            ReferenceType.RequestBody => document.Components.RequestBodies?.ContainsKey(id) == true,
            ReferenceType.Header => document.Components.Headers?.ContainsKey(id) == true,
            ReferenceType.SecurityScheme => document.Components.SecuritySchemes?.ContainsKey(id) == true,
            ReferenceType.Link => document.Components.Links?.ContainsKey(id) == true,
            ReferenceType.Callback => document.Components.Callbacks?.ContainsKey(id) == true,
            _ => false
        };
    }

    internal static IOpenApiReferenceable? ResolveFromDocument(OpenApiDocument document, ReferenceType type, string id)
    {
        if (document.Components == null) return null;

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
}
