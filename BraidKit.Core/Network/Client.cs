using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace BraidKit.Core.Network;

public sealed class Client : IDisposable
{
    private readonly NetManager _netManager;
    private NetPeer? _connectedServer;
    private Player? OwnPlayer { get; set; }
    private readonly Dictionary<PlayerId, Player> _otherPlayers = [];
    private readonly List<ChatMessage> _chatLog = [];
    private int lastPollPacketCount = 0;

    public event Action StartSpeedrunEvent = null!;
    public event Action<ChatMessage> ChatMessageReceivedEvent = null!;

    /// <summary>True when connection to server has been established</summary>
    [MemberNotNullWhen(true, nameof(_connectedServer))]
    public bool IsConnected => _connectedServer is not null;

    /// <summary>True when server has responded</summary>
    [MemberNotNullWhen(true, nameof(_connectedServer), nameof(OwnPlayer))]
    public bool IsGameInitialized => IsConnected && OwnPlayer is not null;

    public Client()
    {
        var listener = new EventBasedNetListener();
        _netManager = new(listener); // Port is chosen by OS
        _netManager.Start();

        listener.PeerConnectedEvent += peer =>
        {
            _connectedServer = peer;
            Console.WriteLine("Client connected to server");
        };

        listener.PeerDisconnectedEvent += (peer, info) =>
        {
            _connectedServer = null;
            OwnPlayer = null;
            Console.WriteLine("Client disconnected from server");

            if (info.Reason is DisconnectReason.ConnectionRejected && info.AdditionalData.AvailableBytes > 0)
            {
                string reason = info.AdditionalData.GetString();
                Console.WriteLine($"Disconnect reason: {reason}");
            }

            // TODO: Reconnect to server automatically after timeout
            //if (info.Reason is DisconnectReason.ConnectionFailed or DisconnectReason.Timeout)
            //    ReconnectToServer();
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            lastPollPacketCount++;
            OnPacketReceived(dataReader, fromPeer);
            dataReader.Recycle();
        };
    }

    public void Dispose()
    {
        _netManager.Stop();
        Console.WriteLine("Client stopped");
    }

    /// <summary>Client doesn't have a "main loop" since we want to poll events at specific times to get predictable behavior</summary>
    public int PollEvents()
    {
        lastPollPacketCount = 0;
        _netManager.PollEvents();

        // Remove stale players
        var stalePlayerIds = _otherPlayers.Values.Where(x => x.Stale).Select(x => x.PlayerId).ToList();
        stalePlayerIds.ForEach(x => _otherPlayers.Remove(x));

        return lastPollPacketCount;
    }

    public List<PlayerSummary> GetPlayers()
    {
        var result = _otherPlayers.Values.Select(x => x.ToSummary()).ToList();
        if (OwnPlayer != null)
            result.Add(OwnPlayer.ToSummary(isOwnPlayer: true));
        return result;
    }

    public List<ChatMessage> GetChat() => [.. _chatLog];

    public PlayerColor GetOwnPlayerColor() => OwnPlayer?.Color ?? KnownColor.White;

    public async Task<bool> ConnectToServer(string serverHostnameOrIpAddress, int serverPort, string playerName = "", PlayerColor playerColor = default, CancellationToken ct = default)
    {
        if (IsConnected)
            return true;

        Console.WriteLine($"Connecting to server {serverHostnameOrIpAddress}:{serverPort}");

        var packet = new PlayerJoinRequestPacket(playerName, playerColor);
        var writer = new NetDataWriter();
        packet.Serialize(writer);

        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts && !IsConnected && !ct.IsCancellationRequested; i++)
        {
            if (i > 0)
                Console.WriteLine($"Retrying (attempt {i + 1} of {maxAttempts})...");

            _netManager.Connect(serverHostnameOrIpAddress, serverPort, writer);

            for (int j = 0; j < 20 && !IsConnected && !ct.IsCancellationRequested; j++)
            {
                await Task.Delay(50, ct);
                PollEvents();
            }
        }

        return IsConnected;
    }

    public void SendPlayerStateUpdate(int? speedrunFrameIndex, int puzzlePieces, EntitySnapshot entitySnapshot)
    {
        if (!IsGameInitialized)
            return;

        // Ignore already sent frame index
        if (entitySnapshot.FrameIndex <= OwnPlayer.EntitySnapshot.FrameIndex)
            return;

        OwnPlayer.SpeedrunFrameIndex = speedrunFrameIndex;
        OwnPlayer.PuzzlePieces = (byte)puzzlePieces;
        OwnPlayer.EntitySnapshot = entitySnapshot;
        OwnPlayer.Updated = DateTime.Now;

        var packet = new PlayerStateUpdatePacket(OwnPlayer.SpeedrunFrameIndex, OwnPlayer.PuzzlePieces, OwnPlayer.EntitySnapshot);
        var writer = new NetDataWriter();
        packet.Serialize(writer);

        _connectedServer.Send(writer, DeliveryMethod.Unreliable);
        _netManager.TriggerUpdate(); // Flush packet immediately
    }

    // TODO: Remove non-responding clients from server instead of continuously sending keep-alive packets
    /// <summary>Updates current player state with new frame index (used to send keep-alive packet when game is paused)</summary>
    public void SendPlayerStateUpdate(int frameIndex)
    {
        if (!IsGameInitialized) return;

        SendPlayerStateUpdate(OwnPlayer.SpeedrunFrameIndex, OwnPlayer.PuzzlePieces, OwnPlayer.EntitySnapshot with { FrameIndex = frameIndex });
    }

    public void SendChatMessage(string message)
    {
        if (!IsGameInitialized)
            return;

        var packet = new PlayerChatMessagePacket(message);
        var writer = new NetDataWriter();
        packet.Serialize(writer);

        _connectedServer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SimulateLatency(int maxLatency)
    {
        _netManager.SimulateLatency = maxLatency > 0;
        _netManager.SimulationMinLatency = maxLatency / 2;
        _netManager.SimulationMaxLatency = maxLatency;
        _netManager.SimulatePacketLoss = maxLatency > 0;
        _netManager.SimulationPacketLossChance = maxLatency > 0 ? 10 : 0;
    }

    private void OnPacketReceived(NetDataReader reader, NetPeer sender)
    {
        if (!PacketParser.TryReadPacket(reader, out var packet))
            return;

        switch (packet)
        {
            case PlayerJoinResponsePacket playerJoinResponsePacket:
                HandlePlayerJoinResponse(playerJoinResponsePacket, sender);
                break;
            case PlayerStateBroadcastPacket playerStateBroadcastPacket:
                HandlePlayerStateBroadcast(playerStateBroadcastPacket);
                break;
            case PlayerChatMessageBroadcastPacket playerChatMessageBroadcastPacket:
                HandlePlayerChatMessageBroadcast(playerChatMessageBroadcastPacket);
                break;
            case StartSpeedrunBroadcastPacket startSpeedrunBroadcastPacket:
                HandleStartSpeedrunBroadcast(startSpeedrunBroadcastPacket);
                break;
            default:
                Console.WriteLine($"Unsupported packet type: {packet.PacketType}");
                break;
        }
    }

    private void HandlePlayerJoinResponse(PlayerJoinResponsePacket packet, NetPeer sender)
    {
        _connectedServer = sender;

        OwnPlayer = new()
        {
            PlayerId = packet.PlayerId,
            Name = packet.PlayerName,
            Color = packet.PlayerColor,
            SpeedrunFrameIndex = default,
            PuzzlePieces = default,
            EntitySnapshot = EntitySnapshot.Empty,
            Updated = DateTime.Now,
        };

        Console.WriteLine("Connected to server");
    }

    private void HandlePlayerStateBroadcast(PlayerStateBroadcastPacket packet)
    {
        if (packet.PlayerId == PlayerId.Unknown)
            return;

        if (_otherPlayers.TryGetValue(packet.PlayerId, out var player))
        {
            // Ignore stale packet
            if (packet.EntitySnapshot.FrameIndex <= player.EntitySnapshot.FrameIndex)
                return;

            player.Name = packet.PlayerName;
            player.Color = packet.PlayerColor;
            player.SpeedrunFrameIndex = packet.SpeedrunFrameIndex;
            player.PuzzlePieces = packet.PuzzlePieces;
            player.EntitySnapshot = packet.EntitySnapshot;
            player.Updated = DateTime.Now;
        }
        else
        {
            player = new Player
            {
                PlayerId = packet.PlayerId,
                Name = packet.PlayerName,
                Color = packet.PlayerColor,
                SpeedrunFrameIndex = packet.SpeedrunFrameIndex,
                PuzzlePieces = packet.PuzzlePieces,
                EntitySnapshot = packet.EntitySnapshot,
                Updated = DateTime.Now,
            };
            _otherPlayers.Add(player.PlayerId, player);

            Console.WriteLine($"Player joined: {player.Name}");
        }
    }

    private void HandlePlayerChatMessageBroadcast(PlayerChatMessageBroadcastPacket packet)
    {
        var message = new ChatMessage(packet.Sender, packet.Message, packet.Color);
        _chatLog.Add(message);

        const int chatLogMaxCount = 10;
        if (_chatLog.Count > chatLogMaxCount)
            _chatLog.RemoveRange(0, _chatLog.Count - chatLogMaxCount);

        ChatMessageReceivedEvent?.Invoke(message);
    }

    private void HandleStartSpeedrunBroadcast(StartSpeedrunBroadcastPacket startSpeedrunBroadcastPacket)
    {
        StartSpeedrunEvent?.Invoke();
    }
}
