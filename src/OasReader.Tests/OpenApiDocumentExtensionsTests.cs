using FluentAssertions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
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
        
        var file = File.OpenRead(openapiFilename);
        var textReader = new StreamReader(file);
        var reader = new OpenApiTextReaderReader();
        var result = await reader.ReadAsync(textReader, CancellationToken.None);        
        OpenApiDocument sut = result.OpenApiDocument;

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
    public async Task ContainsExternalReferences_BeFalse(string apiFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var openapiFilename = Path.Combine(folder, apiFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);
        await File.WriteAllTextAsync(openapiFilename, apiContents);
        
        var file = File.OpenRead(openapiFilename);
        var textReader = new StreamReader(file);
        var reader = new OpenApiTextReaderReader();
        var result = await reader.ReadAsync(textReader, CancellationToken.None);        
        OpenApiDocument sut = result.OpenApiDocument;

        sut.ContainsExternalReferences().Should().BeFalse();
    }
}
