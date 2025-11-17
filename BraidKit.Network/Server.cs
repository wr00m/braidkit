using System.Collections.Immutable;
using System.Drawing;
using System.Net;

namespace BraidKit.Network;

public sealed class Server : IDisposable
{
    private readonly UdpHelper _udpHelper;
    private readonly Dictionary<IPEndPoint, Player> _connectedPlayers = [];
    public List<PlayerSummary> GetPlayers() => [.. _connectedPlayers.Values.Where(x => !x.TimedOut).Select(x => x.ToSummary())];

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
                if (PacketParser.TryParse<PlayerJoinRequestPacket>(data, out var playerJoinRequestPacket))
                    HandlePlayerJoinRequest(playerJoinRequestPacket, sender);
                // TODO: Maybe respond with PlayerJoinResponsePacket.Failed if parse failed (e.g., wrong packet size due to API version mismatch)
                break;
            case PacketType.PlayerStateUpdate:
                if (PacketParser.TryParse<PlayerStateUpdatePacket>(data, out var playerStateUpdatePacket))
                    HandlePlayerStateUpdate(playerStateUpdatePacket, sender);
                break;
            default:
                Console.WriteLine($"Unsupported packet type: {packetType}");
                break;
        }
    }

    private void HandlePlayerJoinRequest(PlayerJoinRequestPacket packet, IPEndPoint sender)
    {
        if (packet.ApiVersion != ApiVersion.Current)
        {
            _udpHelper.SendPacket(PlayerJoinResponsePacket.Failed, sender);
            return;
        }

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
                    SpeedrunFrameIndex = default,
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

        player.SpeedrunFrameIndex = packet.SpeedrunFrameIndex;
        player.PuzzlePieces = packet.PuzzlePieces;
        player.EntitySnapshot = packet.EntitySnapshot;
        player.Updated = DateTime.Now;

        // Broadcast update to all other clients
        var otherPlayers = _connectedPlayers.Keys.Except([sender]).ToList();
        if (otherPlayers.Count > 0)
        {
            var broadcastPacket = new PlayerStateBroadcastPacket(player.PlayerId, player.Name, player.Color, player.SpeedrunFrameIndex, player.PuzzlePieces, player.EntitySnapshot);
            foreach (var otherPlayer in otherPlayers)
                _udpHelper.SendPacket(broadcastPacket, otherPlayer);
        }
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
        var unusedColors = _prioritizedColors.Except(takenColors).ToList();
        return unusedColors.Count > 0 ? unusedColors.GetRandom() : _prioritizedColors.GetRandom();
    }

    private static readonly ImmutableList<KnownColor> _prioritizedColors = [
        KnownColor.Cyan,
        KnownColor.Yellow,
        KnownColor.Green,
        KnownColor.Purple,
        KnownColor.Red,
        KnownColor.Orange,
        KnownColor.Pink,
        KnownColor.Red,
        KnownColor.Blue,
        KnownColor.Gold,
        KnownColor.Magenta,
        KnownColor.Violet,
        KnownColor.Chocolate,
        KnownColor.Teal,
        KnownColor.Aquamarine,
        KnownColor.Khaki,
        KnownColor.White,
    ];
}

internal static class CollectionHelper
{
    public static T? GetRandom<T>(this ICollection<T> items, T? defaultIfEmpty = default)
    {
        return items.Count > 0 ? items.ElementAt(new Random().Next(0, items.Count)) : defaultIfEmpty;
    }
}