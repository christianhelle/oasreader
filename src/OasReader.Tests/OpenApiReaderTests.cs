using FluentAssertions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace OasReader.Tests;

public class OpenApiReaderTests
{
    [Theory]
    [InlineData("bot.yaml", "bot.components.yaml")]
    [InlineData("petstore.yaml", "petstore.components.yaml")]
    public async Task Returns_NotNull(string apiFile, string componentsFile)
    {
        var result = await Arrange(apiFile, componentsFile);
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("bot.yaml", "bot.components.yaml")]
    [InlineData("petstore.yaml", "petstore.components.yaml")]
    public async Task Returns_Components_Schemas_NotNull(string apiFile, string componentsFile)
    {
        OpenApiDocument result = await Arrange(apiFile, componentsFile);
        result.Components.Should().NotBeNull();
        result.Components.Schemas.Should().NotBeNull();
    }

    [Theory]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseGuildID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseChannelID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseChannelCategoryID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseUserID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseRoleID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseMessageID")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseSequenceInChannel")]
    [InlineData("bot.yaml", "bot.components.yaml", "BaseScheduleID")]
    [InlineData("bot.yaml", "bot.components.yaml", "Guild")]
    [InlineData("bot.yaml", "bot.components.yaml", "User")]
    [InlineData("bot.yaml", "bot.components.yaml", "Channel")]
    [InlineData("bot.yaml", "bot.components.yaml", "Member")]
    [InlineData("bot.yaml", "bot.components.yaml", "Role")]
    [InlineData("bot.yaml", "bot.components.yaml", "ChannelPermissions")]
    [InlineData("bot.yaml", "bot.components.yaml", "Message")]
    [InlineData("petstore.yaml", "petstore.components.yaml", "Order")]
    [InlineData("petstore.yaml", "petstore.components.yaml", "Pet")]
    [InlineData("petstore.yaml", "petstore.components.yaml", "Tag")]
    [InlineData("petstore.yaml", "petstore.components.yaml", "Category")]
    [InlineData("petstore.yaml", "petstore.components.yaml", "ApiResponse")]
    public async Task Returns_Document_With_External_Schemas(string apiFile, string componentsFile, string schemaName)
    {
        OpenApiDocument result = await Arrange(apiFile, componentsFile);
        result.Components.Schemas.Should().ContainKey(schemaName);

    }

    [Fact]
    public async Task Returns_Document_With_Remote_External_Schemas()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var openapiFilename = Path.Combine(folder, "remote-petstore.yaml");
        
        // Debug: Check if the embedded resource exists and what it contains
        try
        {
            var apiContents = EmbeddedResources.GetStream("remote-petstore.yaml");
            Console.WriteLine($"Resource content length: {apiContents?.Length ?? 0}");
            Console.WriteLine($"First 100 chars: {apiContents?.Substring(0, Math.Min(100, apiContents?.Length ?? 0))}");
            
            if (string.IsNullOrWhiteSpace(apiContents))
            {
                throw new InvalidOperationException("Embedded resource is empty or null");
            }
            
            await File.WriteAllTextAsync(openapiFilename, apiContents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading embedded resource: {ex.Message}", ex);
        }

        var result = await OpenApiMultiFileReader.Read(openapiFilename);

        result.Should().NotBeNull();
        result.OpenApiDocument.Should().NotBeNull();
        result.ContainedExternalReferences.Should().BeTrue();
        result.OpenApiDocument.Components.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Pet");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Category");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Tag");
    }

    [Fact]
    public async Task Returns_Document_With_Fully_Remote_External_References()
    {
        const string remoteOpenApiUrl = "https://raw.githubusercontent.com/christianhelle/oasreader/refs/heads/main/src/OasReader.Tests/Resources/remote-petstore.yaml";

        var result = await OpenApiMultiFileReader.Read(remoteOpenApiUrl);

        result.Should().NotBeNull();
        result.OpenApiDocument.Should().NotBeNull();
        result.ContainedExternalReferences.Should().BeTrue();
        result.OpenApiDocument.Components.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Pet");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Category");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Tag");
    }

    [Fact]
    public async Task Returns_Document_With_Remote_Relative_External_References()
    {
        const string remoteOpenApiUrl = "https://raw.githubusercontent.com/christianhelle/oasreader/refs/heads/main/src/OasReader.Tests/Resources/relative-remote-petstore.yaml";

        var result = await OpenApiMultiFileReader.Read(remoteOpenApiUrl);

        result.Should().NotBeNull();
        result.OpenApiDocument.Should().NotBeNull();
        result.ContainedExternalReferences.Should().BeTrue();
        result.OpenApiDocument.Components.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().NotBeNull();
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Pet");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Category");
        result.OpenApiDocument.Components.Schemas.Should().ContainKey("Tag");
    }

    private static async Task<OpenApiDocument> Arrange(string apiFile, string componentsFile)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var componentsFilename = Path.Combine(folder, componentsFile);
        var openapiFilename = Path.Combine(folder, apiFile);

        var componentContents = EmbeddedResources.GetStream(componentsFile);
        var apiContents = EmbeddedResources.GetStream(apiFile);

        await File.WriteAllTextAsync(componentsFilename, componentContents);
        await File.WriteAllTextAsync(openapiFilename, apiContents);

        return (await OpenApiMultiFileReader.Read(openapiFilename)).OpenApiDocument;
    }
}
