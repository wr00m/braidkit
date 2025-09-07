namespace BraidKit.Inject.Rendering;

/// <summary>https://github.com/evanw/font-texture-generator</summary>
internal class FontTextureInfo
{
    public const char Newline = '\n';
    public required string Name { get; init; }
    public required int Size { get; init; }
    public required bool Bold { get; init; }
    public required bool Italic { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required Dictionary<char, FontTextureChar> Characters { get; init; }

    public int GetTextWidth(string text) => text.Select(x => Characters.TryGetValue(x, out var charData) ? charData.Advance : 0).Sum();
}

internal class FontTextureChar
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int OriginX { get; init; }
    public required int OriginY { get; init; }
    public required int Advance { get; init; }
}