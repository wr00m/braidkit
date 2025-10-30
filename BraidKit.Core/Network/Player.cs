using BraidKit.Core.Game;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Network;

internal class Player
{
    public required PlayerId PlayerId { get; init; }
    public required int AccessToken { get; init; }
    public required string Name { get; set; }
    public required PlayerColor Color { get; set; }
    public required byte PuzzlePieces { get; set; }
    public required EntitySnapshot EntitySnapshot { get; set; }
    public required DateTime Updated { get; set; }
    public bool IsConnected => PlayerId != PlayerId.Unknown;
    public TimeSpan TimeSinceLastUpdate => DateTime.Now - Updated;
    public bool Stale => TimeSinceLastUpdate > TimeSpan.FromSeconds(2);
    public bool TimedOut => TimeSinceLastUpdate > TimeSpan.FromSeconds(30);
    public PlayerSummary ToSummary(bool isOwnPlayer = false) => new(PlayerId, Name, Color, PuzzlePieces, EntitySnapshot, isOwnPlayer);
}

public record PlayerSummary(PlayerId PlayerId, string Name, PlayerColor Color, int PuzzlePieces, EntitySnapshot EntitySnapshot, bool IsOwnPlayer);

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
    public override string ToString() => KnownColor.ToString();
}
