using System.Drawing;
using System.Reflection;
using System.Text.Json;

namespace BraidKit.Core.Helpers;

public static class EmbeddedResourceHelper
{
    public static string ReadEmbeddedTextFile(this Assembly assembly, string filename)
    {
        using var stream = assembly.GetEmbeddedResourceStream(filename);
        using var reader = new StreamReader(stream);
        var result = reader.ReadToEnd();
        return result;
    }

    public static T ReadEmbeddedJsonFile<T>(this Assembly assembly, string filename)
    {
        using var stream = assembly.GetEmbeddedResourceStream(filename);
        var json = assembly.ReadEmbeddedTextFile(filename);
        var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return result;
    }

    public static Bitmap ReadEmbeddedImageFile(this Assembly assembly, string filename)
    {
        using var stream = assembly.GetEmbeddedResourceStream(filename);
        var result = new Bitmap(stream); // Disposed by caller
        return result;
    }

    private static Stream GetEmbeddedResourceStream(this Assembly assembly, string filename)
    {
        var qualifiedFilename = assembly.GetManifestResourceNames().Single(x => x.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
        var stream = assembly.GetManifestResourceStream(qualifiedFilename)!;
        return stream;
    }
}
