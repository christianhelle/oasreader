using System.Reflection;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using Xunit;

namespace OasReader.Tests;

public class OpenApiReaderTests
{
    [Fact]
    public async Task Returns_NotNull()
    {
        OpenApiDocument result = await Arrange();
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("BaseGuildID")]
    [InlineData("BaseChannelID")]
    [InlineData("BaseChannelCategoryID")]
    [InlineData("BaseUserID")]
    [InlineData("BaseRoleID")]
    [InlineData("BaseMessageID")]
    [InlineData("BaseSequenceInChannel")]
    [InlineData("BaseScheduleID")]
    [InlineData("Guild")]
    [InlineData("User")]
    [InlineData("Channel")]
    [InlineData("Member")]
    [InlineData("Role")]
    [InlineData("ChannelPermissions")]
    [InlineData("Message")]    
    public async Task Returns_Document_With_External_Schemas(string schemaName)
    {
        OpenApiDocument result = await Arrange();
        result.Components.Schemas.Should().ContainKey(schemaName);

    }

    private static async Task<OpenApiDocument> Arrange()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var componentsFilename = Path.Combine(folder, "components.yaml");
        var openapiFilename = Path.Combine(folder, "openapi.yaml");

        await File.WriteAllTextAsync(componentsFilename, EmbeddedResources.Components);
        await File.WriteAllTextAsync(openapiFilename, EmbeddedResources.OpenApi);

        var result = await OpenApiReader.Load(openapiFilename);
        return result;
    }
}

internal class EmbeddedResources
{
    public static string GetStream(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"OasReader.Tests.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Could not find the embedded resource {name}"));

        return reader.ReadToEnd();
    }

    public static string OpenApi => GetStream("openapi.yaml");

    public static string Components => GetStream("components.yaml");
}