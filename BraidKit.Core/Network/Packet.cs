using BraidKit.Core.Helpers;
using LiteNetLib.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Vortice.Mathematics;

namespace BraidKit.Core.Network;

internal enum PacketType : byte
{
    PlayerJoinRequest = 1,
    PlayerJoinResponse,
    PlayerNameAndColorBroadcast,
    PlayerStateUpdate,
    PlayerStateBroadcast,
    PlayerChatMessage,
    PlayerChatMessageBroadcast,
    StartSpeedrunBroadcast,
}

internal interface IPacket
{
    PacketType PacketType { get; }
}

internal interface IPacketable<T> where T : IPacketable<T>
{
    void Serialize(NetDataWriter writer);
    static abstract T Deserialize(NetDataReader reader);
}

public static class PacketConstants
{
    public const int PlayerNameMaxLength = 25;
    public const int ChatMessageMaxLength = 100;
    public const int SpeedrunFrameIndexNotStarted = -1; // If not in speedrun mode
}

internal record PlayerJoinRequestPacket(string PlayerName, Color PlayerColor, byte ApiVersion = ApiVersion.Current)
    : IPacket, IPacketable<PlayerJoinRequestPacket>
{
    public PacketType PacketType => PacketType.PlayerJoinRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(ApiVersion);
        writer.Put(PlayerName, PacketConstants.PlayerNameMaxLength);
        writer.Put(PlayerColor);
    }

    public static PlayerJoinRequestPacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerJoinRequest))
            return default!;

        return new(
            ApiVersion: reader.GetByte(),
            PlayerName: reader.GetString(PacketConstants.PlayerNameMaxLength),
            PlayerColor: reader.GetColor());
    }
}

internal record PlayerJoinResponsePacket(PlayerId PlayerId, string PlayerName, Color PlayerColor)
    : IPacket, IPacketable<PlayerJoinResponsePacket>
{
    public PacketType PacketType => PacketType.PlayerJoinResponse;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(PlayerId);
        writer.Put(PlayerName, PacketConstants.PlayerNameMaxLength);
        writer.Put(PlayerColor);
    }

    public static PlayerJoinResponsePacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerJoinResponse))
            return default!;

        return new(
            PlayerId: reader.GetByte(),
            PlayerName: reader.GetString(PacketConstants.PlayerNameMaxLength),
            PlayerColor: reader.GetColor());
    }
}

internal record PlayerNameAndColorBroadcastPacket(PlayerId PlayerId, string PlayerName, Color PlayerColor)
    : IPacket, IPacketable<PlayerNameAndColorBroadcastPacket>
{
    public PacketType PacketType => PacketType.PlayerNameAndColorBroadcast;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(PlayerId);
        writer.Put(PlayerName, PacketConstants.PlayerNameMaxLength);
        writer.Put(PlayerColor);
    }

    public static PlayerNameAndColorBroadcastPacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerNameAndColorBroadcast))
            return default!;

        return new(
            PlayerId: reader.GetByte(),
            PlayerName: reader.GetString(PacketConstants.PlayerNameMaxLength),
            PlayerColor: reader.GetColor());
    }
}

internal record PlayerStateUpdatePacket(int? SpeedrunFrameIndex, byte PuzzlePieces, EntitySnapshot EntitySnapshot)
    : IPacket, IPacketable<PlayerStateUpdatePacket>
{
    public PacketType PacketType => PacketType.PlayerStateUpdate;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(SpeedrunFrameIndex, PacketConstants.SpeedrunFrameIndexNotStarted);
        writer.Put(PuzzlePieces);
        EntitySnapshot.Serialize(writer);
    }

    public static PlayerStateUpdatePacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerStateUpdate))
            return default!;

        return new(
            SpeedrunFrameIndex: reader.GetNullableInt(PacketConstants.SpeedrunFrameIndexNotStarted),
            PuzzlePieces: reader.GetByte(),
            EntitySnapshot: EntitySnapshot.Deserialize(reader));
    }
}

// TODO: Player name and color shouldn't be included with every update
internal record PlayerStateBroadcastPacket(PlayerId PlayerId, string PlayerName, Color PlayerColor, int? SpeedrunFrameIndex, byte PuzzlePieces, EntitySnapshot EntitySnapshot)
    : IPacket, IPacketable<PlayerStateBroadcastPacket>
{
    public PacketType PacketType => PacketType.PlayerStateBroadcast;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(PlayerId);
        writer.Put(PlayerName, PacketConstants.PlayerNameMaxLength);
        writer.Put(PlayerColor);
        writer.Put(SpeedrunFrameIndex, PacketConstants.SpeedrunFrameIndexNotStarted);
        writer.Put(PuzzlePieces);
        EntitySnapshot.Serialize(writer);
    }

    public static PlayerStateBroadcastPacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerStateBroadcast))
            return default!;

        return new(
            PlayerId: reader.GetByte(),
            PlayerName: reader.GetString(PacketConstants.PlayerNameMaxLength),
            PlayerColor: reader.GetColor(),
            SpeedrunFrameIndex: reader.GetNullableInt(PacketConstants.SpeedrunFrameIndexNotStarted),
            PuzzlePieces: reader.GetByte(),
            EntitySnapshot: EntitySnapshot.Deserialize(reader));
    }
}

internal record PlayerChatMessagePacket(string Message)
    : IPacket, IPacketable<PlayerChatMessagePacket>
{
    public PacketType PacketType => PacketType.PlayerChatMessage;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(Message, PacketConstants.ChatMessageMaxLength);
    }

    public static PlayerChatMessagePacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerChatMessage))
            return default!;

        return new(Message: reader.GetString(PacketConstants.ChatMessageMaxLength));
    }
}

internal record PlayerChatMessageBroadcastPacket(string Sender, string Message, Color Color)
    : IPacket, IPacketable<PlayerChatMessageBroadcastPacket>
{
    public PacketType PacketType => PacketType.PlayerChatMessageBroadcast;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
        writer.Put(Sender, PacketConstants.PlayerNameMaxLength);
        writer.Put(Message, PacketConstants.ChatMessageMaxLength);
        writer.Put(Color);
    }

    public static PlayerChatMessageBroadcastPacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.PlayerChatMessageBroadcast))
            return default!;

        return new(
            Sender: reader.GetString(PacketConstants.PlayerNameMaxLength),
            Message: reader.GetString(PacketConstants.ChatMessageMaxLength),
            Color: reader.GetColor());
    }

    public static PlayerChatMessageBroadcastPacket ServerMessage(string message) => new("Server", message, ColorHelper.White);
}

internal record StartSpeedrunBroadcastPacket()
    : IPacket, IPacketable<StartSpeedrunBroadcastPacket>
{
    public PacketType PacketType => PacketType.StartSpeedrunBroadcast;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)PacketType);
    }

    public static StartSpeedrunBroadcastPacket Deserialize(NetDataReader reader)
    {
        if (!reader.ReadPacketType(PacketType.StartSpeedrunBroadcast))
            return default!;

        return new();
    }
}

internal static class PacketParser
{
    public static bool ReadPacketType(this NetDataReader reader, PacketType expectedPacketType)
    {
        return reader.AvailableBytes > 0 && reader.GetByte() == (byte)expectedPacketType;
    }

    public static bool TryReadPacket(NetDataReader reader, [NotNullWhen(true)] out IPacket? result)
    {
        if (reader.AvailableBytes == 0)
        {
            result = null;
            return false;
        }

        var packetType = (PacketType)reader.PeekByte();

        result = packetType switch
        {
            PacketType.PlayerJoinRequest => PlayerJoinRequestPacket.Deserialize(reader),
            PacketType.PlayerJoinResponse => PlayerJoinResponsePacket.Deserialize(reader),
            PacketType.PlayerNameAndColorBroadcast => PlayerNameAndColorBroadcastPacket.Deserialize(reader),
            PacketType.PlayerStateUpdate => PlayerStateUpdatePacket.Deserialize(reader),
            PacketType.PlayerStateBroadcast => PlayerStateBroadcastPacket.Deserialize(reader),
            PacketType.PlayerChatMessage => PlayerChatMessagePacket.Deserialize(reader),
            PacketType.PlayerChatMessageBroadcast => PlayerChatMessageBroadcastPacket.Deserialize(reader),
            PacketType.StartSpeedrunBroadcast => StartSpeedrunBroadcastPacket.Deserialize(reader),
            _ => null,
        };

        return result != null;
    }

    public static TPacket Read<TPacket>(NetDataReader reader) where TPacket : class, INetSerializable, new()
    {
        var result = new TPacket();
        result.Deserialize(reader);
        return result;
    }

    public static void Put(this NetDataWriter writer, int? value, int sentinelValue)
    {
        writer.Put(value ?? sentinelValue);
    }

    public static int? GetNullableInt(this NetDataReader reader, int sentinelValue)
    {
        var result = reader.GetInt();
        return result != sentinelValue ? result : null;
    }

    public static void Put(this NetDataWriter writer, Color color, bool alpha = false)
    {
        writer.Put(color.R);
        writer.Put(color.G);
        writer.Put(color.B);

        if (alpha)
            writer.Put(color.A);
    }

    public static Color GetColor(this NetDataReader reader, bool alpha = false)
    {
        return new(reader.GetByte(), reader.GetByte(), reader.GetByte(), alpha ? reader.GetByte() : (byte)255);
    }
}

public record EntitySnapshot(int FrameIndex, byte World, byte Level, Vector2 Position, bool FacingLeft, byte AnimationIndex, float AnimationTime)
    : IPacketable<EntitySnapshot>
{
    public static EntitySnapshot Empty => new(
        FrameIndex: default,
        World: default,
        Level: default,
        Position: default,
        FacingLeft: default,
        AnimationIndex: default,
        AnimationTime: default);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(FrameIndex);
        writer.Put(World);
        writer.Put(Level);
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(FacingLeft);
        writer.Put(AnimationIndex);
        writer.Put(AnimationTime);
    }

    public static EntitySnapshot Deserialize(NetDataReader reader)
    {
        return new(
            FrameIndex: reader.GetInt(),
            World: reader.GetByte(),
            Level: reader.GetByte(),
            Position: new(reader.GetFloat(), reader.GetFloat()),
            FacingLeft: reader.GetBool(),
            AnimationIndex: reader.GetByte(),
            AnimationTime: reader.GetFloat());
    }
}