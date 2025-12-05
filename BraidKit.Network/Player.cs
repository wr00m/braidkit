using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BraidKit.Network;

internal class Player
{
    public required PlayerId PlayerId { get; init; }
    public required int AccessToken { get; init; }
    public required string Name { get; set; }
    public required PlayerColor Color { get; set; }
    public required uint SpeedrunFrameIndex { get; set; } // Frame count since start of speedrun (if speedrun mode is active)
    public required byte PuzzlePieces { get; set; }
    public required EntitySnapshot EntitySnapshot { get; set; }
    public required DateTime Updated { get; set; }

    public bool IsConnected => PlayerId != PlayerId.Unknown;
    public TimeSpan TimeSinceLastUpdate => DateTime.Now - Updated;
    public bool Stale => TimeSinceLastUpdate > TimeSpan.FromSeconds(2);
    public bool TimedOut => TimeSinceLastUpdate > TimeSpan.FromSeconds(30);
    public PlayerSummary ToSummary(bool isOwnPlayer = false) => new(PlayerId, Name, Color, SpeedrunFrameIndex, PuzzlePieces, EntitySnapshot, isOwnPlayer);
}

public record PlayerSummary(PlayerId PlayerId, string Name, PlayerColor Color, uint SpeedrunFrameIndex, int PuzzlePieces, EntitySnapshot EntitySnapshot, bool IsOwnPlayer)
{
    public string FormatSpeedrunTime()
    {
        const int fps = 60;
        int totalHundredths = ((int)SpeedrunFrameIndex * 100 + fps / 2) / fps; // Round to nearest
        int minutes = totalHundredths / (60 * 100);
        int seconds = (totalHundredths / 100) % 60;
        int hundredths = totalHundredths % 100;
        return $"{minutes}:{seconds:D2}.{hundredths:D2}";
    }
}

public static class PlayerExtensions
{
    // TODO: This is kinda crude, should probably order by PuzzlePieces and then by SpeedrunFrameIndex for most recent puzzle piece
    public static IEnumerable<PlayerSummary> OrderByLeaderboardPosition(this IEnumerable<PlayerSummary> items) => items
        .OrderByDescending(x => x.PuzzlePieces)
        .ThenByDescending(x => x.EntitySnapshot.World)
        .ThenByDescending(x => x.EntitySnapshot.Level)
        .ThenBy(x => x.SpeedrunFrameIndex)
        .ThenBy(x => x.Name)
        .ThenBy(x => x.PlayerId.Value);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerId(byte Value)
{
    public static readonly PlayerId Unknown = default;
    public static readonly PlayerId MinValue = 1;
    public static readonly PlayerId MaxValue = 255;
    public static implicit operator byte(PlayerId x) => x.Value;
    public static implicit operator PlayerId(byte x) => new(x);
    public override string ToString() => Value.ToString();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerColor(KnownColor KnownColor)
{
    public static readonly PlayerColor Undefined = default;
    public static implicit operator PlayerColor(KnownColor x) => new(x);
    public static implicit operator Color(PlayerColor x) => Color.FromKnownColor(x.KnownColor);
    public uint ToRgba() { Color c = this; return (uint)((c.R) | (c.G << 8) | (c.B << 16) | (c.A << 24)); }
    public Vector4 ToVector4() { Color c = this; return new Vector4(c.R, c.G, c.B, c.A) / 255f; }
    public string ToHex(string prefix = "#") { Color c = this; return $"{prefix}{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}"; }
    public override string ToString() => KnownColor.ToString();
}
