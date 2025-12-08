using FluentAssertions;
using Microsoft.OpenApi.Models;
using OasReader.Visitors;
using Xunit;

namespace OasReader.Tests;

public class NullHandlingTests
{
    [Fact]
    public void ReferenceCache_Add_WithNullReference_DoesNotThrow()
    {
        // Arrange
        var cache = new ReferenceCache();
        var schema = new OpenApiSchema
        {
            Reference = null
        };

        // Act
        var act = () => cache.Add(schema);

        // Assert
        act.Should().NotThrow();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void ReferenceCache_Add_WithNullReferenceId_DoesNotThrow()
    {
        // Arrange
        var cache = new ReferenceCache();
        var schema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = null,
                Type = ReferenceType.Schema
            }
        };

        // Act
        var act = () => cache.Add(schema);

        // Assert
        act.Should().NotThrow();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void ReferenceCache_Add_WithValidReference_AddsToCache()
    {
        // Arrange
        var cache = new ReferenceCache();
        var schema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = "TestSchema",
                Type = ReferenceType.Schema
            }
        };

        // Act
        cache.Add(schema);

        // Assert
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void MergeExternalReferences_WithNullComponents_DoesNotThrow()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths(),
            Components = null
        };
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var tempFile = Path.Combine(folder, "test.yaml");
        File.WriteAllText(tempFile, "openapi: 3.0.0");

        try
        {
            // Act
            var act = () => document.MergeExternalReferences(tempFile);

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void MergeExternalReferences_WithNullSchemas_DoesNotThrow()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents()
        };
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var tempFile = Path.Combine(folder, "test.yaml");
        File.WriteAllText(tempFile, "openapi: 3.0.0");

        try
        {
            // Act
            var act = () => document.MergeExternalReferences(tempFile);

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void MergeExternalReferences_WithEmptyDocument_ReturnsValidDocument()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var tempFile = Path.Combine(folder, "test.yaml");
        File.WriteAllText(tempFile, "openapi: 3.0.0");

        try
        {
            // Act
            var result = document.MergeExternalReferences(tempFile);

            // Assert
            result.Should().NotBeNull();
            result.Info.Title.Should().Be("Test");
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }
}
