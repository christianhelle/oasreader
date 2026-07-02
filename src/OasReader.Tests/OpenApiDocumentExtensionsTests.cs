using System.Text;
using FluentAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace OasReader.Tests;

public class OpenApiDocumentExtensionsTests
{
    [Theory]
    [InlineData("bot.yaml", "bot.components.yaml")]
    [InlineData("petstore.yaml", "petstore.components.yaml")]
    public async Task ContainsExternalReferences_BeTrue(string apiFile, string componentsFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var componentsFilename = Path.Combine(folder, componentsFile);
        var openapiFilename = Path.Combine(folder, apiFile);

        var componentContents = EmbeddedResources.GetStream(componentsFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);

        await File.WriteAllTextAsync(componentsFilename, componentContents);
        await File.WriteAllTextAsync(openapiFilename, apiContents);

        using var file = File.OpenRead(openapiFilename);
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(file, settings: settings);
        result.Document.Should().NotBeNull();
        var sut = result.Document!;

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Theory]
    [InlineData("v2.api-with-examples.yaml")]
    [InlineData("v2.petstore-expanded.yaml")]
    [InlineData("v2.petstore-minimal.yaml")]
    [InlineData("v2.petstore-simple.yaml")]
    [InlineData("v2.petstore-with-external-docs.yaml")]
    [InlineData("v2.petstore.yaml")]
    [InlineData("v2.uber.yaml")]
    [InlineData("v2.api-with-examples.json")]
    [InlineData("v2.petstore-expanded.json")]
    [InlineData("v2.petstore-minimal.json")]
    [InlineData("v2.petstore-simple.json")]
    [InlineData("v2.petstore-with-external-docs.json")]
    [InlineData("v2.petstore.json")]
    [InlineData("v2.uber.json")]
    [InlineData("v3.api-with-examples.yaml")]
    [InlineData("v3.api-with-examples.json")]
    [InlineData("v3.callback-example.yaml")]
    [InlineData("v3.callback-example.json")]
    [InlineData("v3.hubspot-events.json")]
    [InlineData("v3.hubspot-webhooks.json")]
    [InlineData("v3.ingram-micro.json")]
    [InlineData("v3.link-example.yaml")]
    [InlineData("v3.link-example.json")]
    [InlineData("v3.petstore-expanded.yaml")]
    [InlineData("v3.petstore-expanded.json")]
    [InlineData("v3.petstore.yaml")]
    [InlineData("v3.petstore.json")]
    [InlineData("v3.uspto.yaml")]
    [InlineData("v3.uspto.json")]
    [InlineData("v3.no-content.yaml")]
    public async Task ContainsExternalReferences_BeFalse(string apiFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var openapiFilename = Path.Combine(folder, apiFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);
        await File.WriteAllTextAsync(openapiFilename, apiContents);

        using var file = File.OpenRead(openapiFilename);
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        var result = await OpenApiDocument.LoadAsync(file, settings: settings);
        result.Document.Should().NotBeNull();

        result.Document!.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WhenPathsIsNull()
    {
        var sut = new OpenApiDocument { Paths = null };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithPathItemParameterSchemaExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                parameters:
                  - name: PetId
                    in: query
                    schema:
                      $ref: 'components.yaml#/components/schemas/Pet'
                get:
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithPathItemParameterLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                parameters:
                  - name: Limit
                    in: query
                    schema:
                      type: integer
                get:
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithOperationParameterSchemaExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  parameters:
                    - name: PetId
                      in: query
                      schema:
                        $ref: 'components.yaml#/components/schemas/Pet'
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithOperationParameterLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  parameters:
                    - name: Limit
                      in: query
                      schema:
                        type: integer
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithRequestBodyContentExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                post:
                  requestBody:
                    content:
                      application/json:
                        schema:
                          $ref: 'components.yaml#/components/schemas/Pet'
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithResponseHeaderExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  responses:
                    '200':
                      description: ok
                      headers:
                        X-RateLimit:
                          schema:
                            $ref: 'components.yaml#/components/schemas/Pet'
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithPathItemParameterExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                parameters:
                  - $ref: 'components.yaml#/components/parameters/PetId'
                get:
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithPathItemParameterContentExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                parameters:
                  - name: PetId
                    in: query
                    content:
                      application/json:
                        schema:
                          $ref: 'components.yaml#/components/schemas/Pet'
                get:
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithOperationParameterExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  parameters:
                    - $ref: 'components.yaml#/components/parameters/PetId'
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeTrue_WithOperationParameterContentExternalRef()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  parameters:
                    - name: PetId
                      in: query
                      content:
                        application/json:
                          schema:
                            $ref: 'components.yaml#/components/schemas/Pet'
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeTrue();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithResponseHeaderLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  responses:
                    '200':
                      description: ok
                      headers:
                        X-RateLimit:
                          schema:
                            type: integer
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithRequestBodyLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                post:
                  requestBody:
                    content:
                      application/json:
                        schema:
                          type: object
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WithPathItemNullParameter()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Parameters = new List<IOpenApiParameter> { null! }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WithOperationNullParameter()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = new List<IOpenApiParameter> { null! }
                        }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WithNullResponseValue()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses { ["200"] = null! }
                        }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WithNullContentValue()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType> { ["application/json"] = null! }
                                }
                            }
                        }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WithNullHeaderValue()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Headers = new Dictionary<string, IOpenApiHeader> { ["X-RateLimit"] = null! }
                                }
                            }
                        }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithResponseContentLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
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
                            type: object
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public async Task ContainsExternalReferences_BeFalse_WithOperationParameterContentLocalSchema()
    {
        var sut = await LoadDocumentFromTextAsync("""
            openapi: 3.0.1
            info:
              title: Test
              version: "1.0"
            paths:
              /pets:
                get:
                  parameters:
                    - name: PetId
                      in: query
                      content:
                        application/json:
                          schema:
                            type: object
                  responses:
                    '200':
                      description: ok
            """);

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WhenOperationResponsesIsNull()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation { Responses = null! }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Fact]
    public void ContainsExternalReferences_BeFalse_WhenRequestBodyContentIsNull()
    {
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody()
                        }
                    }
                }
            }
        };

        sut.ContainsExternalReferences().Should().BeFalse();
    }

    [Theory]
    [InlineData("https://developers.intellihr.io/docs/v1/swagger.json")] // GZIP encoded
    [InlineData("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    public async Task ContainsExternalReferences_BeFalse_WhenUrl(string url)
    {
        var sut = await OpenApiMultiFileReader.Read(url);
        sut.OpenApiDocument.ContainsExternalReferences().Should().BeFalse();
    }

    [Theory]
    [InlineData("v3.ingram-micro.json")]
    [InlineData("v3.weather.json")]
    public async Task ValidationRuleSet_Default(string apiFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var openapiFilename = Path.Combine(folder, apiFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);
        await File.WriteAllTextAsync(openapiFilename, apiContents);

        var result = await OpenApiMultiFileReader.Read(openapiFilename, ValidationRuleSet.GetDefaultRuleSet());

        result.OpenApiDiagnostic.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("v2.api-with-examples.yaml")]
    [InlineData("v2.petstore-expanded.yaml")]
    [InlineData("v2.petstore-minimal.yaml")]
    [InlineData("v2.petstore-simple.yaml")]
    [InlineData("v2.petstore-with-external-docs.yaml")]
    [InlineData("v2.petstore.yaml")]
    [InlineData("v2.uber.yaml")]
    [InlineData("v2.api-with-examples.json")]
    [InlineData("v2.petstore-expanded.json")]
    [InlineData("v2.petstore-minimal.json")]
    [InlineData("v2.petstore-simple.json")]
    [InlineData("v2.petstore-with-external-docs.json")]
    [InlineData("v2.petstore.json")]
    [InlineData("v2.uber.json")]
    [InlineData("v3.api-with-examples.yaml")]
    [InlineData("v3.api-with-examples.json")]
    [InlineData("v3.callback-example.yaml")]
    [InlineData("v3.callback-example.json")]
    [InlineData("v3.hubspot-events.json")]
    [InlineData("v3.hubspot-webhooks.json")]
    [InlineData("v3.ingram-micro.json")]
    [InlineData("v3.link-example.yaml")]
    [InlineData("v3.link-example.json")]
    [InlineData("v3.petstore-expanded.yaml")]
    [InlineData("v3.petstore-expanded.json")]
    [InlineData("v3.petstore.yaml")]
    [InlineData("v3.petstore.json")]
    [InlineData("v3.uspto.yaml")]
    [InlineData("v3.uspto.json")]
    [InlineData("v3.no-content.yaml")]
    [InlineData("v3.weather.json")]
    public async Task ValidationRuleSet_Empty(string apiFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var openapiFilename = Path.Combine(folder, apiFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);
        await File.WriteAllTextAsync(openapiFilename, apiContents);

        var result = await OpenApiMultiFileReader.Read(openapiFilename, ValidationRuleSet.GetEmptyRuleSet());

        result.OpenApiDiagnostic.Errors.Should().BeEmpty();
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
}
