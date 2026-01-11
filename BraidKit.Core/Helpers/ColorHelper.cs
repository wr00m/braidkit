using System.Buffers.Binary;
using System.Globalization;
using Vortice.Mathematics;
using Color = Vortice.Mathematics.Color;
using SDColor = System.Drawing.Color;

namespace BraidKit.Core.Helpers;

public static class ColorHelper
{
    public static readonly Color Empty = default;
    public static readonly Color White = SDColor.White.ToVortice();
    public static readonly Color Cyan = SDColor.Cyan.ToVortice();

    public static bool IsSameColor(this Color a, Color b, bool ignoreAlpha = false)
        => a.R == b.R && a.G == b.G && a.B == b.B && (a.A == b.A || ignoreAlpha);

    public static string ToHex(this Color x, string prefix = "")
        => $"{prefix}{x.R:x2}{x.G:x2}{x.B:x2}{x.A:x2}";

    public static Color ToColor(this Color4 x) => x.ToRgba();

    public static bool TryParseColor(string str, out Color result) => TryParseColorName(str, out result) || TryParseColorHex(str, out result);

    private static bool TryParseColorName(string name, out Color result)
    {
        var namedColor = SDColor.FromName(name.Trim());
        if (namedColor.IsKnownColor)
        {
            result = namedColor.ToVortice();
            return true;
        }

        result = Empty;
        return false;
    }

    private static bool TryParseColorHex(string hex, out Color result)
    {
        hex = hex.Trim().TrimStart('#');

        if (hex.Length is 3 or 4)
            hex = string.Concat(hex.Select(x => $"{x}{x}")); // fff -> ffffff

        if (hex.Length is 6)
            hex += "ff"; // Use alpha=255 as default

        if (hex.Length is 8 && uint.TryParse(hex, NumberStyles.HexNumber, null, out uint abgr))
        {
            result = new Color(BinaryPrimitives.ReverseEndianness(abgr));
            return true;
        }

        result = Empty;
        return false;
    }

    private static Color ToVortice(this SDColor x) => new(x.R, x.G, x.B, x.A);
}
