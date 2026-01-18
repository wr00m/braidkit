using LiteNetLib;
using LiteNetLib.Utils;

namespace BraidKit.Core.Network;

internal static class NetworkHelper
{
    public static void RejectWithMessage(this ConnectionRequest connectionRequest, string message)
    {
        var writer = new NetDataWriter();
        writer.Put(message);
        connectionRequest.Reject(writer);
    }

    public static void SendPacket<T>(this NetPeer client, T packet, DeliveryMethod deliveryMethod) where T : IPacket, IPacketable<T>
    {
        client.Send(packet.ToWriter(), deliveryMethod);
    }

    public static void SendServerMessage(this NetPeer client, string message)
    {
        var packet = new ServerMessagePacket(message);
        client.SendPacket(packet, DeliveryMethod.ReliableUnordered);
    }

    public static NetDataWriter ToWriter<T>(this T packet) where T : IPacketable<T>
    {
        var writer = new NetDataWriter();
        packet.Serialize(writer);
        return writer;
    }
}
