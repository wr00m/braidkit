namespace BraidKit.Core.Helpers;

public static class StringHelper
{
    public static string Truncate(this string x, int maxLength) => x.Length <= maxLength ? x : x[..maxLength];
}
