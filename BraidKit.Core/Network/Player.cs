using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace BraidKit.Core.Network;

internal class Player
{
    public required PlayerId PlayerId { get; init; }
    public required string Name { get; set; }
    public required Color Color { get; set; }
    public required int? SpeedrunFrameIndex { get; set; } // Frame count since start of speedrun (if speedrun mode is active)
    public required byte PuzzlePieces { get; set; }
    public required EntitySnapshot EntitySnapshot { get; set; }
    public required DateTime Updated { get; set; }

    public bool IsConnected => PlayerId != PlayerId.Unknown;
    public bool IsInSpeedrunMode => SpeedrunFrameIndex is not null;
    public TimeSpan TimeSinceLastUpdate => DateTime.Now - Updated;
    public bool Stale => TimeSinceLastUpdate > TimeSpan.FromSeconds(2);
    public PlayerSummary ToSummary(bool isOwnPlayer = false, int ping = 0) => new(PlayerId, Name, Color, SpeedrunFrameIndex, PuzzlePieces, EntitySnapshot, isOwnPlayer, ping);
}

public record PlayerSummary(PlayerId PlayerId, string Name, Color Color, int? SpeedrunFrameIndex, int PuzzlePieces, EntitySnapshot EntitySnapshot, bool IsOwnPlayer, int Ping)
{
    public string FormatSpeedrunTime()
    {
        if (SpeedrunFrameIndex is null)
            return "";

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
