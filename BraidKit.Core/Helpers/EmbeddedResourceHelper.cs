using System.Reflection;

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

    private static Stream GetEmbeddedResourceStream(this Assembly assembly, string filename)
    {
        var qualifiedFilename = assembly.GetManifestResourceNames().Single(x => x.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
        var stream = assembly.GetManifestResourceStream(qualifiedFilename)!;
        return stream;
    }
}
