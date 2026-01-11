using BraidKit.Core.Helpers;
using LiteNetLib;
using LiteNetLib.Utils;
using Vortice.Mathematics;

namespace BraidKit.Core.Network;

public sealed class Server : IDisposable
{
    private readonly NetManager _netManager;
    private readonly Dictionary<int, Player> _connectedPlayers = []; // Key is NetPeer id
    public List<PlayerSummary> GetPlayers() => [.. _connectedPlayers.Select(x => x.Value.ToSummary(ping: _netManager.GetPeerById(x.Key)?.Ping ?? -1))];
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

        listener.PeerDisconnectedEvent += (peer, info) => RemovePlayer(peer.Id);

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
        // TODO: We shouldn't need high precision timer here once client implements frame interpolation/extrapolation
        using var highPrecisionTimer = OperatingSystem.IsWindows() ? new HighPrecisionTimer(5) : null;

        while (_netManager.IsRunning && !ct.IsCancellationRequested)
        {
            _netManager.PollEvents();

            // TODO: Why is this necessary? Sometimes NetPeer seems to disappear without PeerDisconnectedEvent
            var missingPeerIds = _connectedPlayers.Keys.Where(x => !_netManager.TryGetPeerById(x, out _)).ToList();
            missingPeerIds.ForEach(RemovePlayer);

            // Delay before polling again (longer delay if no players are connected)
            await Task.Delay(_connectedPlayers.Count > 0 ? 5 : 200, ct);
        }
    }

    private void RemovePlayer(int peerId)
    {
        if (_connectedPlayers.Remove(peerId, out var disconnectedPlayer))
            Console.WriteLine($"Player disconnected: {disconnectedPlayer.Name}");
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
                Console.WriteLine($"Unhandled packet: {packet.PacketType}");
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

                player = new()
                {
                    PlayerId = playerId,
                    Name = IsValidPlayerName(packet.PlayerName) ? packet.PlayerName.Trim() : GetUniquePlayerName(),
                    Color = IsValidPlayerColor(packet.PlayerColor) ? packet.PlayerColor : GetRandomPlayerColor(),
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
        var otherPlayers = _connectedPlayers.Keys.Except([sender.Id]).Select(x => _netManager.GetPeerById(x)).Where(x => x != null).ToList();
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

        if (HandleChatMessageCommand(playerChatMessagePacket.Message, sender))
        {
            Console.WriteLine($"Command from {player.Name}: {playerChatMessagePacket.Message}");
            return;
        }

        Console.WriteLine($"Chat message from {player.Name}: {playerChatMessagePacket.Message}");

        var broadcastPacket = new PlayerChatMessageBroadcastPacket(player.Name, player.PlayerId, playerChatMessagePacket.Message, player.Color);
        var broadcastWriter = new NetDataWriter();
        broadcastPacket.Serialize(broadcastWriter);
        _netManager.SendToAll(broadcastWriter, DeliveryMethod.ReliableOrdered);
    }

    private bool HandleChatMessageCommand(string command, NetPeer sender)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var commandName = parts[0];
        var commandArgs = parts.Length > 1 ? parts[1] : null;
        var player = _connectedPlayers.TryGetValue(sender.Id, out var p) ? p : null;

        switch (commandName)
        {
            case "!start":
                if (commandArgs is null)
                {
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
                }
                return true;
            case "!disconnect":
                if (commandArgs is null)
                    sender.Disconnect();
                return true;
            case "!name":
                var playerName = (commandArgs?.Trim() ?? "").Truncate(PacketConstants.PlayerNameMaxLength);
                if (IsValidPlayerName(playerName) && player != null)
                {
                    player.Name = playerName;
                    var packet = new PlayerNameAndColorBroadcastPacket(player.PlayerId, player.Name, player.Color);
                    var writer = new NetDataWriter();
                    packet.Serialize(writer);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableSequenced);
                }
                return true;
            case "!color":
                var playerColor = commandArgs is null ? GetRandomPlayerColor() : ColorHelper.TryParseColor(commandArgs, out var c) ? c : ColorHelper.Empty;
                if (IsValidPlayerColor(playerColor) && player != null)
                {
                    player.Color = playerColor;
                    var packet = new PlayerNameAndColorBroadcastPacket(player.PlayerId, player.Name, player.Color);
                    var writer = new NetDataWriter();
                    packet.Serialize(writer);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableSequenced);
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

    private string GetUniquePlayerName()
    {
        var takenNames = _connectedPlayers.Values.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; ; i++)
        {
            var suffix = i > 1 ? $"_{i}" : "";
            var name = $"Tim{suffix}";

            if (!takenNames.Contains(name))
                return name;
        }
    }

    private static Color GetRandomPlayerColor()
    {
        var hue = Random.Shared.GetRandomFloat(0f, 1f);
        var saturation = Random.Shared.GetRandomFloat(.6f, 1f);
        var lightness = Random.Shared.GetRandomFloat(.7f, .9f);
        return Color4.FromHSL(hue, saturation, lightness);
    }

    private static bool IsValidPlayerName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= PacketConstants.PlayerNameMaxLength;
    private static bool IsValidPlayerColor(Color color) => !color.IsSameColor(ColorHelper.Empty, ignoreAlpha: true);
}
