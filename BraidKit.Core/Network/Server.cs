using BraidKit.Core.Game;
using System.Collections.Immutable;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Network;

public sealed class Server : IDisposable
{
    private readonly UdpHelper _udpHelper;
    private readonly Dictionary<IPEndPoint, Player> _connectedPlayers = [];

    // TODO: Re-enqueue disconnected player ids and/or kick stalest player out when we run out of ids

    public Server(int port)
    {
        _udpHelper = new(port, OnPacketReceived);
        Console.WriteLine($"Server started on port {port}. Press Ctrl+C to exit.\n");
    }

    public void Dispose()
    {
        _udpHelper.Dispose();
        Console.WriteLine("Server stopped");
    }

    private void OnPacketReceived(byte[] data, IPEndPoint sender)
    {
        if (data.Length == 0)
            return;

        var packetType = (PacketType)data[0];
        switch (packetType)
        {
            case PacketType.PlayerJoinRequest:
                HandlePlayerJoinRequest(MemoryMarshal.Read<PlayerJoinRequestPacket>(data), sender);
                break;
            case PacketType.PlayerStateUpdate:
                HandlePlayerStateUpdate(MemoryMarshal.Read<PlayerStateUpdatePacket>(data), sender);
                break;
            default:
                Console.WriteLine($"Unsupported packet type: {packetType}");
                break;
        }
    }

    private void HandlePlayerJoinRequest(PlayerJoinRequestPacket packet, IPEndPoint sender)
    {
        // Handle race condition when multiple players join at the same time
        lock (_connectedPlayers)
        {
            // Remove timed out players to free up player ids and colors (there's probably a more suitable place to do this)
            var timedOutPlayerKeys = _connectedPlayers.Where(x => x.Value.TimedOut).Select(x => x.Key).ToList();
            foreach (var timedOutPlayerKey in timedOutPlayerKeys)
                if (_connectedPlayers.Remove(timedOutPlayerKey, out var removedPlayer))
                    Console.WriteLine($"Removed timed out player {removedPlayer.Name}");

            // Add player if not already connected
            if (!_connectedPlayers.TryGetValue(sender, out var player))
            {
                // Deny join request if unable to get a unique player id
                if (!TryGetNextUniquePlayerId(out var playerId))
                {
                    Console.WriteLine("Failed to generate player id");
                    _udpHelper.SendPacket(PlayerJoinResponsePacket.Failed, sender);
                    return;
                }

                var playerColor = packet.PlayerColor != PlayerColor.Undefined ? packet.PlayerColor : GetPreferablyUniqueColor();

                player = new()
                {
                    PlayerId = playerId,
                    AccessToken = new Random().Next(1, int.MaxValue),
                    Name = !string.IsNullOrWhiteSpace(packet.PlayerName) ? packet.PlayerName : $"{playerColor.KnownColor} Tim",
                    Color = playerColor,
                    PuzzlePieces = default,
                    EntitySnapshot = EntitySnapshot.Empty,
                    Updated = DateTime.Now,
                };

                _connectedPlayers.Add(sender, player);

                Console.WriteLine($"Player joined: {player.Name}");
            }

            // Send response packet (resend if player is already connected)
            _udpHelper.SendPacket(new PlayerJoinResponsePacket(player.PlayerId, player.Name, player.AccessToken, player.Color), sender);
        }
    }

    private void HandlePlayerStateUpdate(PlayerStateUpdatePacket packet, IPEndPoint sender)
    {
        // Ignore packet if sender isn't connected
        if (!_connectedPlayers.TryGetValue(sender, out var player))
            return;

        // Ignore packet if wrong player id
        if (packet.PlayerId != player.PlayerId)
            return;

        // Ignore packet if wrong access token (prevent spoofing and ignore stale connections)
        if (packet.AccessToken != player.AccessToken)
            return;

        // Ignore packet if stale
        if (packet.EntitySnapshot.FrameIndex <= player.EntitySnapshot.FrameIndex)
            return;

        player.PuzzlePieces = packet.PuzzlePieces;
        player.EntitySnapshot = packet.EntitySnapshot;
        player.Updated = DateTime.Now;

        // Broadcast update to all other clients
        var otherPlayers = _connectedPlayers.Keys.Except([sender]).ToList();
        if (otherPlayers.Count > 0)
        {
            var broadcastPacket = new PlayerStateBroadcastPacket(player.PlayerId, player.Name, player.Color, player.PuzzlePieces, player.EntitySnapshot);
            foreach (var otherPlayer in otherPlayers)
                _udpHelper.SendPacket(broadcastPacket, otherPlayer);
        }

        //Console.WriteLine($"Player updated: X={packet.PlayerPosition.X:0.##} Y={packet.PlayerPosition.Y:0.##}");
    }

    private bool TryGetNextUniquePlayerId(out PlayerId result)
    {
        var takenIds = _connectedPlayers.Values.Select(x => x.PlayerId.Value).ToHashSet();
        result = Enumerable.Range(PlayerId.MinValue, PlayerId.MaxValue).Select(x => (byte)x).FirstOrDefault(x => !takenIds.Contains(x), PlayerId.Unknown);
        return result != PlayerId.Unknown;
    }

    private PlayerColor GetPreferablyUniqueColor()
    {
        var takenColors = _connectedPlayers.Values.Select(x => x.Color.KnownColor).ToHashSet();
        var result = _prioritizedColors.FirstOrDefault(x => !takenColors.Contains(x), KnownColor.White);
        return result;
    }

    // TODO: Rework this list with bright, easily readable colors
    private static readonly ImmutableList<KnownColor> _prioritizedColors = [
        KnownColor.OrangeRed,
        KnownColor.Cyan,
        KnownColor.Yellow,
        KnownColor.Green,
        KnownColor.Purple,
        KnownColor.Orange,
        KnownColor.Pink,
        KnownColor.Red,
        KnownColor.Blue,
    ];
}
