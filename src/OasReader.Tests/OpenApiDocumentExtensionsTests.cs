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
    public async Task Returns_NotNull(string apiFile, string componentsFile)
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
}
