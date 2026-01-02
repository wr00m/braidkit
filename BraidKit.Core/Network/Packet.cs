using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BraidKit.Core.Network;

internal enum PacketType : byte
{
    PlayerJoinRequest = 1,
    PlayerJoinResponse,
    PlayerStateUpdate,
    PlayerStateBroadcast,
    PlayerSpeedrunStarted,
    PlayerSpeedrunStartedBroadcast,
    PlayerChatMessage,
    PlayerChatMessageBroadcast,
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
internal unsafe readonly struct PlayerJoinResponsePacket(PlayerId playerId, FixedLengthAsciiString playerName, PlayerColor playerColor)
{
    public readonly PacketType PacketType = PacketType.PlayerJoinResponse;
    public readonly byte ApiVersion = Network.ApiVersion.Current;
    public readonly PlayerId PlayerId = playerId;
    public readonly PlayerColor PlayerColor = playerColor;
    public readonly FixedLengthAsciiString PlayerName = playerName;
    public bool Accepted => PlayerId != PlayerId.Unknown;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerStateUpdatePacket(uint speedrunFrameIndex, byte puzzlePieces, EntitySnapshot entitySnapshot)
{
    public readonly PacketType PacketType = PacketType.PlayerStateUpdate;
    public readonly uint SpeedrunFrameIndex = speedrunFrameIndex;
    public readonly byte PuzzlePieces = puzzlePieces;
    public readonly EntitySnapshot EntitySnapshot = entitySnapshot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerStateBroadcastPacket(PlayerId playerId, FixedLengthAsciiString playerName, PlayerColor playerColor, uint speedrunFrameIndex, byte puzzlePieces, EntitySnapshot entitySnapshot)
{
    public readonly PacketType PacketType = PacketType.PlayerStateBroadcast;
    public readonly PlayerId PlayerId = playerId;
    public readonly PlayerColor PlayerColor = playerColor;
    public readonly FixedLengthAsciiString PlayerName = playerName;
    public readonly uint SpeedrunFrameIndex = speedrunFrameIndex;
    public readonly byte PuzzlePieces = puzzlePieces;
    public readonly EntitySnapshot EntitySnapshot = entitySnapshot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerSpeedrunStartedPacket()
{
    public readonly PacketType PacketType = PacketType.PlayerSpeedrunStarted;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerSpeedrunStartedBroadcastPacket()
{
    public readonly PacketType PacketType = PacketType.PlayerSpeedrunStartedBroadcast;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerChatMessagePacket(string message)
{
    public readonly PacketType PacketType = PacketType.PlayerChatMessage;
    public readonly string Message = message.Trim();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct PlayerChatMessageBroadcastPacket(PlayerId playerId, string message)
{
    public readonly PacketType PacketType = PacketType.PlayerChatMessageBroadcast;
    public readonly PlayerId PlayerId = playerId;
    public readonly string Message = message.Trim();
}

internal static class PacketParser
{
    public static bool TryParse<TPacket>(byte[] data, out TPacket result) where TPacket : unmanaged
    {
        var expectedSize = Unsafe.SizeOf<TPacket>();
        if (data.Length != expectedSize)
        {
            result = default;
            return false;
        }

        result = MemoryMarshal.Read<TPacket>(data);
        return true;
    }
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