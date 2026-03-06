using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OasReader.Visitors;
using Xunit;

namespace OasReader.Tests;

public class InternalBehaviorTests
{
    [Fact]
    public async Task MergeExternalReferencesAsStringAsync_ReturnsYaml_WhenInputIsYaml()
    {
        var folder = CreateTemporaryFolder();
        var openApiPath = Path.Combine(folder, "petstore.yaml");
        var componentsPath = Path.Combine(folder, "petstore.components.yaml");

        await File.WriteAllTextAsync(openApiPath, EmbeddedResources.GetStream("petstore.yaml"));
        await File.WriteAllTextAsync(componentsPath, EmbeddedResources.GetStream("petstore.components.yaml"));

        var document = await LoadDocumentAsync(openApiPath);

        var merged = await document.MergeExternalReferencesAsStringAsync(openApiPath);

        merged.Should().Contain("components:");
        merged.Should().Contain("Pet:");
    }

    [Fact]
    public async Task MergeExternalReferencesAsStringAsync_ReturnsJson_WhenInputIsJson()
    {
        var folder = CreateTemporaryFolder();
        var openApiPath = Path.Combine(folder, "weather.json");

        await File.WriteAllTextAsync(openApiPath, EmbeddedResources.GetStream("v3.weather.json"));

        var document = await LoadDocumentAsync(openApiPath);

        var merged = await document.MergeExternalReferencesAsStringAsync(openApiPath);

        merged.TrimStart().Should().StartWith("{");
        merged.Should().Contain("\"openapi\"");
    }

    [Fact]
    public async Task Read_ThrowsInvalidOperation_WhenLocalFileDoesNotExist()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}", "missing.yaml");

        var action = async () => await OpenApiMultiFileReader.Read(missingPath);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Could not open the file at {missingPath}*");
    }

    [Fact]
    public async Task Read_ThrowsInvalidOperation_WhenHttpDownloadFails()
    {
        const string url = "http://127.0.0.1:1/missing.yaml";

        var action = async () => await OpenApiMultiFileReader.Read(url);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Could not download the file at {url}*");
    }

    [Fact]
    public async Task Read_ReturnsNullDocument_WhenParsedDocumentIsMissing()
    {
        var folder = CreateTemporaryFolder();
        var openApiPath = Path.Combine(folder, "invalid.yaml");

        await File.WriteAllTextAsync(openApiPath, "{}");

        var result = await OpenApiMultiFileReader.Read(openApiPath);

        ((object?)result.OpenApiDocument).Should().BeNull();
        result.ContainedExternalReferences.Should().BeFalse();
        result.OpenApiDiagnostic.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void OpenApiReferenceExtensions_UpdateExternalReference_ToLocalReference()
    {
        var schemaReference = new OpenApiSchemaReference("Pet", new OpenApiDocument(), "components.yaml");

        schemaReference.HasExternalReference().Should().BeTrue();
        schemaReference.GetBaseReference().Should().NotBeNull();

        schemaReference.SetLocalReference("Pet", ReferenceType.Schema);

        schemaReference.HasExternalReference().Should().BeFalse();
        schemaReference.Reference.Id.Should().Be("Pet");
        schemaReference.Reference.Type.Should().Be(ReferenceType.Schema);
        schemaReference.Reference.ExternalResource.Should().BeNull();
        OpenApiReferenceExtensions.HasExternalReference(new object()).Should().BeFalse();
    }

    [Fact]
    public void OpenApiReferenceExtensions_SetLocalReference_Returns_WhenBaseReferenceIsMissing()
    {
        var holder = new FakeReferenceHolder();

        var action = () => holder.SetLocalReference("Pet", ReferenceType.Schema);

        action.Should().NotThrow();
    }

    [Fact]
    public void ComponentResolver_Supports_AllDocumentComponentTypes()
    {
        var schema = new OpenApiSchema();
        var response = new OpenApiResponse();
        var parameter = new OpenApiParameter();
        var example = new OpenApiExample();
        var requestBody = new OpenApiRequestBody();
        var header = new OpenApiHeader();
        var securityScheme = new OpenApiSecurityScheme();
        var link = new OpenApiLink();
        var callback = new OpenApiCallback();

        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema> { ["Pet"] = schema },
                Responses = new Dictionary<string, IOpenApiResponse> { ["Ok"] = response },
                Parameters = new Dictionary<string, IOpenApiParameter> { ["TraceId"] = parameter },
                Examples = new Dictionary<string, IOpenApiExample> { ["PetExample"] = example },
                RequestBodies = new Dictionary<string, IOpenApiRequestBody> { ["CreatePet"] = requestBody },
                Headers = new Dictionary<string, IOpenApiHeader> { ["RateLimit"] = header },
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme> { ["Bearer"] = securityScheme },
                Links = new Dictionary<string, IOpenApiLink> { ["NextPage"] = link },
                Callbacks = new Dictionary<string, IOpenApiCallback> { ["OnPet"] = callback },
            },
        };

        AssertComponent(document, ReferenceType.Schema, "Pet", schema);
        AssertComponent(document, ReferenceType.Response, "Ok", response);
        AssertComponent(document, ReferenceType.Parameter, "TraceId", parameter);
        AssertComponent(document, ReferenceType.Example, "PetExample", example);
        AssertComponent(document, ReferenceType.RequestBody, "CreatePet", requestBody);
        AssertComponent(document, ReferenceType.Header, "RateLimit", header);
        AssertComponent(document, ReferenceType.SecurityScheme, "Bearer", securityScheme);
        AssertComponent(document, ReferenceType.Link, "NextPage", link);
        AssertComponent(document, ReferenceType.Callback, "OnPet", callback);

        ComponentResolver.ExistsInDocument(new OpenApiDocument(), ReferenceType.Schema, "Pet").Should().BeFalse();
        ComponentResolver.ExistsInDocument(document, ReferenceType.Tag, "Pets").Should().BeFalse();
        ComponentResolver.ResolveFromDocument(document, ReferenceType.Tag, "Pets").Should().BeNull();
    }

    [Fact]
    public void ReferenceCache_UpdateDocument_PopulatesAllSupportedCollections()
    {
        var cache = new ReferenceCache();
        var schema = new OpenApiSchema();
        var response = new OpenApiResponse();
        var parameter = new OpenApiParameter();
        var example = new OpenApiExample();
        var requestBody = new OpenApiRequestBody();
        var header = new OpenApiHeader();
        var securityScheme = new OpenApiSecurityScheme();
        var link = new OpenApiLink();
        var callback = new OpenApiCallback();
        var tag = new OpenApiTag { Name = "Pets" };

        cache.Add(ReferenceType.Schema, "Pet", schema);
        cache.Add(ReferenceType.Schema, "Pet", new OpenApiSchema());
        cache.Add(ReferenceType.Response, "Ok", response);
        cache.Add(ReferenceType.Parameter, "TraceId", parameter);
        cache.Add(ReferenceType.Example, "PetExample", example);
        cache.Add(ReferenceType.RequestBody, "CreatePet", requestBody);
        cache.Add(ReferenceType.Header, "RateLimit", header);
        cache.Add(ReferenceType.SecurityScheme, "Bearer", securityScheme);
        cache.Add(ReferenceType.Link, "NextPage", link);
        cache.Add(ReferenceType.Callback, "OnPet", callback);
        cache.Add(ReferenceType.Tag, tag.Name!, tag);

        var document = new OpenApiDocument();

        cache.UpdateDocument(document);

        cache.Count.Should().Be(10);
        document.Components.Should().NotBeNull();
        document.Components!.Schemas.Should().ContainKey("Pet").WhoseValue.Should().BeSameAs(schema);
        document.Components.Responses.Should().ContainKey("Ok").WhoseValue.Should().BeSameAs(response);
        document.Components.Parameters.Should().ContainKey("TraceId").WhoseValue.Should().BeSameAs(parameter);
        document.Components.Examples.Should().ContainKey("PetExample").WhoseValue.Should().BeSameAs(example);
        document.Components.RequestBodies.Should().ContainKey("CreatePet").WhoseValue.Should().BeSameAs(requestBody);
        document.Components.Headers.Should().ContainKey("RateLimit").WhoseValue.Should().BeSameAs(header);
        document.Components.SecuritySchemes.Should().ContainKey("Bearer").WhoseValue.Should().BeSameAs(securityScheme);
        document.Components.Links.Should().ContainKey("NextPage").WhoseValue.Should().BeSameAs(link);
        document.Components.Callbacks.Should().ContainKey("OnPet").WhoseValue.Should().BeSameAs(callback);
        document.Tags.Should().Contain(tag);
    }

    [Fact]
    public void ReferenceCache_UpdateDocument_Throws_ForUnsupportedReferenceType()
    {
        var cache = new ReferenceCache();
        cache.Add((ReferenceType)999, "Unknown", new OpenApiSchema());

        var action = () => cache.UpdateDocument(new OpenApiDocument());

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task OpenApiReferenceResolverVisitor_ResolvesExternalSchemas_FromLocalFiles()
    {
        var folder = CreateTemporaryFolder();
        var openApiPath = Path.Combine(folder, "openapi.yaml");
        var componentsPath = Path.Combine(folder, "components.yaml");

        await File.WriteAllTextAsync(openApiPath, CreateOpenApiWithExternalReference("components.yaml#/components/schemas/Pet"));
        await File.WriteAllTextAsync(componentsPath, CreateComponentsDocument());

        var document = await LoadDocumentAsync(openApiPath);
        var visitor = new OpenApiReferenceResolverVisitor(openApiPath, new Dictionary<string, OpenApiDocument>());

        new OpenApiWalker(visitor).Walk(document);
        visitor.Cache.UpdateDocument(document);

        visitor.Cache.Count.Should().Be(1);
        document.Components.Should().NotBeNull();
        document.Components!.Schemas.Should().ContainKey("Pet");
    }

    [Fact]
    public async Task OpenApiReferenceResolverVisitor_IgnoresMissingExternalFiles()
    {
        var folder = CreateTemporaryFolder();
        var openApiPath = Path.Combine(folder, "openapi.yaml");

        await File.WriteAllTextAsync(openApiPath, CreateOpenApiWithExternalReference("missing-components.yaml#/components/schemas/Pet"));

        var document = await LoadDocumentAsync(openApiPath);
        var visitor = new OpenApiReferenceResolverVisitor(openApiPath, new Dictionary<string, OpenApiDocument>());

        new OpenApiWalker(visitor).Walk(document);

        visitor.Cache.Count.Should().Be(0);
        document.Components.Should().BeNull();
    }

    [Fact]
    public async Task OpenApiMissingReferenceVisitor_AddsMissingSchemas_FromCachedDocuments()
    {
        var document = await LoadDocumentFromTextAsync(CreateOpenApiWithLocalReference());
        var cachedDocument = await LoadDocumentFromTextAsync(CreateComponentsDocument());
        var visitor = new OpenApiMissingReferenceVisitor(
            document,
            new Dictionary<string, OpenApiDocument> { ["components.yaml"] = cachedDocument });

        new OpenApiWalker(visitor).Walk(document);
        visitor.Cache.UpdateDocument(document);

        visitor.Cache.Count.Should().Be(1);
        document.Components.Should().NotBeNull();
        document.Components!.Schemas.Should().ContainKey("Pet");
    }

    [Fact]
    public void OpenApiMissingReferenceVisitor_Returns_WhenReferenceIsMissing()
    {
        var visitor = new OpenApiMissingReferenceVisitor(new OpenApiDocument(), new Dictionary<string, OpenApiDocument>());

        visitor.Visit(new FakeReferenceHolder());

        visitor.Cache.Count.Should().Be(0);
    }

    [Fact]
    public void OpenApiMissingReferenceVisitor_Returns_WhenReferenceIdIsMissing()
    {
        var visitor = new OpenApiMissingReferenceVisitor(new OpenApiDocument(), new Dictionary<string, OpenApiDocument>());
        var holder = new FakeReferenceHolder
        {
            Reference = new BaseOpenApiReference
            {
                Id = string.Empty,
                Type = ReferenceType.Schema,
            },
        };

        visitor.Visit(holder);

        visitor.Cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task OpenApiMissingReferenceVisitor_Continues_WhenCacheDocumentThrows()
    {
        var document = await LoadDocumentFromTextAsync(CreateOpenApiWithLocalReference());
        var visitor = new OpenApiMissingReferenceVisitor(
            document,
            new Dictionary<string, OpenApiDocument>
            {
                ["broken"] = null!,
            });

        new OpenApiWalker(visitor).Walk(document);

        visitor.Cache.Count.Should().Be(0);
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_GetDocument_ReturnsNull_ForWhitespaceReference()
    {
        var visitor = new OpenApiReferenceResolverVisitor("openapi.yaml", new Dictionary<string, OpenApiDocument>());
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("GetDocument", BindingFlags.Instance | BindingFlags.NonPublic);

        var document = method!.Invoke(visitor, ["   "]);

        document.Should().BeNull();
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_GetDocument_ReturnsNull_WhenAbsoluteHttpReferenceFails()
    {
        var visitor = new OpenApiReferenceResolverVisitor("openapi.yaml", new Dictionary<string, OpenApiDocument>());
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("GetDocument", BindingFlags.Instance | BindingFlags.NonPublic);

        var document = method!.Invoke(visitor, ["http://127.0.0.1:1/missing.yaml"]);

        document.Should().BeNull();
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_GetDocument_ReturnsNull_WhenRelativeHttpReferenceFails()
    {
        var visitor = new OpenApiReferenceResolverVisitor("http://127.0.0.1:1/openapi.yaml", new Dictionary<string, OpenApiDocument>());
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("GetDocument", BindingFlags.Instance | BindingFlags.NonPublic);

        var document = method!.Invoke(visitor, ["components.yaml"]);

        document.Should().BeNull();
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_GetDocumentFromStream_ReturnsNull_WhenReadFails()
    {
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("GetDocumentFromStream", BindingFlags.Static | BindingFlags.NonPublic);

        var document = method!.Invoke(null, [new ThrowingStream()]);

        document.Should().BeNull();
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_TryLoadDocument_UsesCachedDocument()
    {
        var cachedDocument = new OpenApiDocument();
        var documentCache = new Dictionary<string, OpenApiDocument>
        {
            ["components.yaml"] = cachedDocument,
        };
        var visitor = new OpenApiReferenceResolverVisitor("openapi.yaml", documentCache);
        var reference = new BaseOpenApiReference
        {
            ExternalResource = "components.yaml",
        };
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("TryLoadDocument", BindingFlags.Instance | BindingFlags.NonPublic);
        var parameters = new object?[] { reference, null };

        var loaded = (bool)method!.Invoke(visitor, parameters)!;

        loaded.Should().BeTrue();
        parameters[1].Should().BeSameAs(cachedDocument);
    }

    [Fact]
    public void OpenApiReferenceResolverVisitor_TryLoadDocument_ReturnsFalse_WhenReferenceIsNotExternal()
    {
        var visitor = new OpenApiReferenceResolverVisitor("openapi.yaml", new Dictionary<string, OpenApiDocument>());
        var reference = new BaseOpenApiReference();
        var method = typeof(OpenApiReferenceResolverVisitor).GetMethod("TryLoadDocument", BindingFlags.Instance | BindingFlags.NonPublic);
        var parameters = new object?[] { reference, null };

        var loaded = (bool)method!.Invoke(visitor, parameters)!;

        loaded.Should().BeFalse();
        parameters[1].Should().BeNull();
    }

    [Fact]
    public async Task OpenApiReferenceResolverVisitor_Returns_WhenReferenceIdIsMissing()
    {
        var cachedDocument = await LoadDocumentFromTextAsync(CreateComponentsDocument());
        var visitor = new OpenApiReferenceResolverVisitor(
            "openapi.yaml",
            new Dictionary<string, OpenApiDocument> { ["components.yaml"] = cachedDocument });
        var holder = new FakeReferenceHolder
        {
            Reference = new BaseOpenApiReference
            {
                Id = string.Empty,
                Type = ReferenceType.Schema,
                ExternalResource = "components.yaml",
            },
        };

        visitor.Visit(holder);

        visitor.Cache.Count.Should().Be(0);
    }

    private static void AssertComponent(
        OpenApiDocument document,
        ReferenceType type,
        string id,
        IOpenApiReferenceable expected)
    {
        ComponentResolver.ExistsInDocument(document, type, id).Should().BeTrue();
        ComponentResolver.ResolveFromDocument(document, type, id).Should().BeSameAs(expected);
    }

    private static string CreateTemporaryFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static async Task<OpenApiDocument> LoadDocumentAsync(string path)
    {
        using var stream = File.OpenRead(path);
        var settings = new OpenApiReaderSettings
        {
            BaseUrl = new Uri($"file://{Path.GetDirectoryName(path)}{Path.DirectorySeparatorChar}"),
        };
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(stream, settings: settings);
        result.Document.Should().NotBeNull();
        return result.Document!;
    }

    private static async Task<OpenApiDocument> LoadDocumentFromTextAsync(string contents)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(stream, settings: settings);
        result.Document.Should().NotBeNull();
        return result.Document!;
    }

    private static string CreateOpenApiWithExternalReference(string componentFile) =>
        $$"""
        openapi: 3.0.1
        info:
          title: Example
          version: "1.0"
        paths:
          /pets:
            get:
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        $ref: '{{componentFile}}'
        """;

    private static string CreateOpenApiWithLocalReference() =>
        """
        openapi: 3.0.1
        info:
          title: Example
          version: "1.0"
        paths:
          /pets:
            get:
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Pet'
        """;

    private static string CreateComponentsDocument() =>
        """
        openapi: 3.0.1
        info:
          title: Components
          version: "1.0"
        components:
          schemas:
            Pet:
              type: object
              properties:
                id:
                  type: string
        """;

    private sealed class ThrowingStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("boom");
    }

    private sealed class FakeReferenceHolder : IOpenApiReferenceHolder
    {
        public BaseOpenApiReference? Reference { get; init; }
        public bool UnresolvedReference => false;
        public void SerializeAsV2(IOpenApiWriter writer) { }
        public void SerializeAsV3(IOpenApiWriter writer) { }
        public void SerializeAsV31(IOpenApiWriter writer) { }
        public void SerializeAsV32(IOpenApiWriter writer) { }
    }
}
