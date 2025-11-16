using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace BraidKit.Network;

internal enum PacketType : byte
{
    PlayerJoinRequest = 1,
    PlayerJoinResponse,
    PlayerStateUpdate,
    PlayerStateBroadcast,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerJoinRequestPacket(FixedLengthAsciiString playerName, PlayerColor playerColor = default)
{
    public readonly PacketType PacketType = PacketType.PlayerJoinRequest;
    public readonly byte ApiVersion = Network.ApiVersion.Current;
    public readonly FixedLengthAsciiString PlayerName = playerName;
    public readonly PlayerColor PlayerColor = playerColor;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerJoinResponsePacket(PlayerId playerId, FixedLengthAsciiString playerName, int accessToken = default, PlayerColor playerColor = default)
{
    public readonly PacketType PacketType = PacketType.PlayerJoinResponse;
    public readonly byte ApiVersion = Network.ApiVersion.Current;
    public readonly PlayerId PlayerId = playerId;
    public readonly int AccessToken = accessToken;
    public readonly PlayerColor PlayerColor = playerColor;
    public readonly FixedLengthAsciiString PlayerName = playerName;
    public bool Accepted => PlayerId != PlayerId.Unknown && AccessToken != default;
    public static PlayerJoinResponsePacket Failed => new(PlayerId.Unknown, "");
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerStateUpdatePacket(PlayerId playerId, int accessToken, byte puzzlePieces, EntitySnapshot entitySnapshot)
{
    public readonly PacketType PacketType = PacketType.PlayerStateUpdate;
    public readonly PlayerId PlayerId = playerId;
    public readonly int AccessToken = accessToken;
    public readonly byte PuzzlePieces = puzzlePieces;
    public readonly EntitySnapshot EntitySnapshot = entitySnapshot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerStateBroadcastPacket(PlayerId playerId, FixedLengthAsciiString playerName, PlayerColor playerColor, byte puzzlePieces, EntitySnapshot entitySnapshot)
{
    public readonly PacketType PacketType = PacketType.PlayerStateBroadcast;
    public readonly PlayerId PlayerId = playerId;
    public readonly PlayerColor PlayerColor = playerColor;
    public readonly FixedLengthAsciiString PlayerName = playerName;
    public readonly byte PuzzlePieces = puzzlePieces;
    public readonly EntitySnapshot EntitySnapshot = entitySnapshot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct EntitySnapshot(int FrameIndex, byte World, byte Level, Vector2 Position, bool FacingLeft, byte AnimationIndex, float AnimationTime)
{
    public static readonly EntitySnapshot Empty = default;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct FixedLengthAsciiString()
{
    private const int MaxBytes = 16;
    private fixed byte AsciiBytes[MaxBytes];

    public static implicit operator string(FixedLengthAsciiString fixedLengthStr)
    {
        var result = Encoding.ASCII.GetString(fixedLengthStr.AsciiBytes, MaxBytes).Trim();
        return result;
    }

    public static implicit operator FixedLengthAsciiString(string str)
    {
        var asciiBytes = Encoding.ASCII.GetBytes(str.Trim());

        if (asciiBytes.Length > MaxBytes)
            asciiBytes = asciiBytes[..MaxBytes];

        var result = new FixedLengthAsciiString();

        var span = new Span<byte>(result.AsciiBytes, MaxBytes);
        span.Fill((byte)' ');
        asciiBytes.CopyTo(span);

        return result;
    }

    public override readonly string ToString() => (string)this;
}