using System.Reflection;

namespace BraidKit.Core.Helpers;

public static class EmbeddedResourceHelper
{
    public static string ReadEmbeddedResourceFile(this Assembly assembly, string filename)
    {
        var qualifiedFilename = assembly.GetManifestResourceNames().Single(x => x.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(qualifiedFilename)!;
        using var reader = new StreamReader(stream);
        var result = reader.ReadToEnd();
        return result;
    }
}
