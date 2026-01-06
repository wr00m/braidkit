using BraidKit.Core.Helpers;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;

namespace BraidKit.Core.Network;

public sealed class Server : IDisposable
{
    private readonly NetManager _netManager;
    private readonly Dictionary<int, Player> _connectedPlayers = []; // Key is NetPeer id
    public List<PlayerSummary> GetPlayers() => [.. _connectedPlayers.Where(x => !x.Value.TimedOut).Select(x => x.Value.ToSummary(ping: _netManager.GetPeerById(x.Key)!.Ping))];
    public int Port => _netManager.LocalPort;

    public Server(int port)
    {
        var listener = new EventBasedNetListener();
        _netManager = new(listener);
        _netManager.Start(port);

        listener.ConnectionRequestEvent += request =>
        {
            const int clientMaxCount = 100;
            if (_netManager.ConnectedPeersCount >= clientMaxCount)
            {
                request.Reject(); // TODO: "Too many connections"
                return;
            }

            if (!PacketParser.TryReadPacket(request.Data, out var packet) || packet is not PlayerJoinRequestPacket playerJoinRequestPacket)
            {
                request.Reject(); // TODO: "Invalid join request"
                return;
            }

            if (playerJoinRequestPacket.ApiVersion != ApiVersion.Current)
            {
                request.Reject(); // TODO: $"API version mismatch (client: {playerJoinRequestPacket.ApiVersion}, server: {ApiVersion.Current})"
                return;
            }

            var peer = request.Accept();

            // TODO: What if HandlePlayerJoinRequest returns false?
            HandlePlayerJoinRequest(playerJoinRequestPacket, peer);
        };

        listener.PeerConnectedEvent += peer =>
        {
            // Send response packet
            if (_connectedPlayers.TryGetValue(peer.Id, out var player))
            {
                var packet = new PlayerJoinResponsePacket(player.PlayerId, player.Name, player.Color);
                var writer = new NetDataWriter();
                packet.Serialize(writer);

                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        };

        listener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (_connectedPlayers.Remove(peer.Id, out var disconnectedPlayer))
                Console.WriteLine($"Player disconnected: {disconnectedPlayer.Name}");
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            OnPacketReceived(dataReader, fromPeer);
            dataReader.Recycle();
        };
    }

    public void Dispose()
    {
        _netManager.Stop();
        Console.WriteLine("Server stopped");
    }

    public async Task MainLoop(CancellationToken ct)
    {
        // TODO: We shouldn't need high precision timer here once client implements frame interpolation
        using var highPrecisionTimer = OperatingSystem.IsWindows() ? new HighPrecisionTimer(5) : null;

        var pingStopwatch = Stopwatch.StartNew();

        while (_netManager.IsRunning && !ct.IsCancellationRequested)
        {
            _netManager.PollEvents();

            // Remove timed out players to free up player ids and colors
            var timedOutPlayerKeys = _connectedPlayers.Where(x => x.Value.TimedOut).Select(x => x.Key).ToList();
            foreach (var timedOutPlayerKey in timedOutPlayerKeys)
                if (_connectedPlayers.Remove(timedOutPlayerKey, out var removedPlayer))
                    Console.WriteLine($"Removed timed out player {removedPlayer.Name}");

            if (pingStopwatch.ElapsedMilliseconds > 5000)
            {
                _netManager.SendToAll([], DeliveryMethod.ReliableUnordered);
                pingStopwatch.Restart();
            }

            await Task.Delay(_connectedPlayers.Count > 0 ? 5 : 500, ct);
        }
    }

    private void OnPacketReceived(NetDataReader reader, NetPeer sender)
    {
        if (!PacketParser.TryReadPacket(reader, out var packet))
            return;

        switch (packet)
        {
            case PlayerStateUpdatePacket playerStateUpdatePacket:
                HandlePlayerStateUpdate(playerStateUpdatePacket, sender);
                break;
            case PlayerChatMessagePacket playerChatMessagePacket:
                HandlePlayerChatMessage(playerChatMessagePacket, sender);
                break;
            default:
                Console.WriteLine($"Unsupported packet type: {packet.PacketType}");
                break;
        }
    }

    private bool HandlePlayerJoinRequest(PlayerJoinRequestPacket packet, NetPeer sender)
    {
        if (packet.ApiVersion != ApiVersion.Current)
            return false;

        // Handle race condition when multiple players join at the same time
        // TODO: We don't actually need this lock if we keep NetManager.UnsyncedEvents set to false 
        lock (_connectedPlayers)
        {
            // Add player if not already connected
            if (!_connectedPlayers.TryGetValue(sender.Id, out var player))
            {
                // Deny join request if unable to get a unique player id
                if (!TryGetNextUniquePlayerId(out var playerId))
                {
                    Console.WriteLine("Failed to generate player id");
                    return false;
                }

                var playerColor = packet.PlayerColor != PlayerColor.Undefined ? packet.PlayerColor : GetPreferablyUniqueColor();

                player = new()
                {
                    PlayerId = playerId,
                    Name = !string.IsNullOrWhiteSpace(packet.PlayerName) ? packet.PlayerName : $"{playerColor.KnownColor} Tim",
                    Color = playerColor,
                    SpeedrunFrameIndex = default,
                    PuzzlePieces = default,
                    EntitySnapshot = EntitySnapshot.Empty,
                    Updated = DateTime.Now,
                };

                _connectedPlayers.Add(sender.Id, player);

                Console.WriteLine($"Player joined: {player.Name}");
            }

            return true;
        }
    }

    private void HandlePlayerStateUpdate(PlayerStateUpdatePacket packet, NetPeer sender)
    {
        // Ignore packet if sender isn't connected
        if (!_connectedPlayers.TryGetValue(sender.Id, out var player))
            return;

        // Ignore packet if stale
        if (packet.EntitySnapshot.FrameIndex <= player.EntitySnapshot.FrameIndex)
            return;

        player.SpeedrunFrameIndex = packet.SpeedrunFrameIndex;
        player.PuzzlePieces = packet.PuzzlePieces;
        player.EntitySnapshot = packet.EntitySnapshot;
        player.Updated = DateTime.Now;

        // Broadcast update to all other clients
        var otherPlayers = _connectedPlayers.Keys.Except([sender.Id]).Select(x => _netManager.GetPeerById(x)!).ToList();
        if (otherPlayers.Count > 0)
        {
            var broadcastPacket = new PlayerStateBroadcastPacket(player.PlayerId, player.Name, player.Color, player.SpeedrunFrameIndex, player.PuzzlePieces, player.EntitySnapshot);
            var broadcastWriter = new NetDataWriter();
            broadcastPacket.Serialize(broadcastWriter);

            foreach (var otherPlayer in otherPlayers)
                otherPlayer.Send(broadcastWriter, DeliveryMethod.Unreliable);

            _netManager.TriggerUpdate(); // Flush packets immediately
        }
    }

    private void HandlePlayerChatMessage(PlayerChatMessagePacket playerChatMessagePacket, NetPeer sender)
    {
        if (!_connectedPlayers.TryGetValue(sender.Id, out var player))
            return;

        if (HandleChatMessageCommand(playerChatMessagePacket.Message))
        {
            Console.WriteLine($"Command from {player.Name}: {playerChatMessagePacket.Message}");
            return;
        }

        Console.WriteLine($"Chat message from {player.Name}: {playerChatMessagePacket.Message}");

        var broadcastPacket = new PlayerChatMessageBroadcastPacket(player.Name, playerChatMessagePacket.Message, player.Color);
        var broadcastWriter = new NetDataWriter();
        broadcastPacket.Serialize(broadcastWriter);
        _netManager.SendToAll(broadcastWriter, DeliveryMethod.ReliableOrdered);
    }

    private bool HandleChatMessageCommand(string command)
    {
        switch (command)
        {
            case "!start":
                if (_connectedPlayers.Any(x => x.Value.IsInSpeedrunMode))
                {
                    var packet = PlayerChatMessageBroadcastPacket.ServerMessage("Cannot start synchronized speedrun because a player is already in speedrun mode");
                    var writer = new NetDataWriter();
                    packet.Serialize(writer);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableUnordered);
                }
                else
                {
                    var packet = new StartSpeedrunBroadcastPacket();
                    var writer = new NetDataWriter();
                    packet.Serialize(writer);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableUnordered);
                }
                return true;
            default:
                return false;
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

public static class CollectionHelper
{
    public static T? GetRandom<T>(this ICollection<T> items, T? defaultIfEmpty = default)
    {
        return items.Count > 0 ? items.ElementAt(new Random().Next(0, items.Count)) : defaultIfEmpty;
    }
}
