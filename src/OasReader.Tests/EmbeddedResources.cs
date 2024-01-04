using System.Reflection;

namespace OasReader.Tests;

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
}