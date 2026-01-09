using System.Buffers.Binary;
using System.Globalization;
using Vortice.Mathematics;

namespace BraidKit.Core.Helpers;

public static class ColorHelper
{
    public static readonly Color Empty = new(0, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255, 255);
    public static readonly Color Cyan = new(0, 255, 255, 255);

    public static bool IsSameColor(this Color a, Color b, bool ignoreAlpha = false)
        => a.R == b.R && a.G == b.G && a.B == b.B && (a.A == b.A || ignoreAlpha);

    public static string ToHex(this Color x, string prefix = "")
        => $"{prefix}{x.R:x2}{x.G:x2}{x.B:x2}{x.A:x2}";

    public static bool TryParseHex(string hex, out Color result)
    {
        hex = hex.Trim().TrimStart('#');

        if (hex.Length is 6)
            hex += "ff"; // Use alpha=255 as default

        if (hex.Length is not 8 || !uint.TryParse(hex, NumberStyles.HexNumber, null, out uint abgr))
        {
            result = Empty;
            return false;
        }

        result = new Color(BinaryPrimitives.ReverseEndianness(abgr));
        return true;
    }
}
