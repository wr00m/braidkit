using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace BraidKit.Network;

public sealed class Client : IDisposable
{
    private readonly UdpHelper _udpHelper;
    private readonly IPEndPoint _serverEndpoint;
    private readonly Dictionary<PlayerId, Player> _otherPlayers = [];
    private Player? OwnPlayer { get; set; }

    [MemberNotNullWhen(true, nameof(OwnPlayer))]
    public bool IsConnected => OwnPlayer?.IsConnected == true;

    public Client(IPAddress serverIP, int serverPort)
    {
        _udpHelper = new(0, OnPacketReceived); // Port is chosen by OS
        _serverEndpoint = new(serverIP, serverPort);
        Console.WriteLine($"Client connecting to server {serverIP}:{serverPort}");
    }

    public void Dispose()
    {
        _udpHelper.Dispose();
        Console.WriteLine("Client stopped");
    }

    public List<PlayerSummary> GetPlayers()
    {
        var result = _otherPlayers.Values.Select(x => x.ToSummary()).ToList();
        if (OwnPlayer != null)
            result.Add(OwnPlayer.ToSummary(true));
        return result;
    }

    public async Task<bool> ConnectToServer(string playerName = "", PlayerColor playerColor = default, CancellationToken ct = default)
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts && !IsConnected && !ct.IsCancellationRequested; i++)
        {
            Console.WriteLine("Connecting to server...");
            SendPlayerJoinRequest(playerName, playerColor);
            await Task.Delay(1000, ct);
        }

        return IsConnected;
    }

    private void SendPlayerJoinRequest(string playerName, PlayerColor playerColor)
    {
        if (IsConnected)
            return;

        var packet = new PlayerJoinRequestPacket(playerName, playerColor);
        _udpHelper.SendPacket(packet, _serverEndpoint);
    }

    public void SendPlayerStateUpdate(uint speedrunFrameIndex, int puzzlePieces, EntitySnapshot entitySnapshot)
    {
        if (!IsConnected)
            return;

        // Ignore already sent frame index
        if (entitySnapshot.FrameIndex <= OwnPlayer.EntitySnapshot.FrameIndex)
            return;

        OwnPlayer.SpeedrunFrameIndex = speedrunFrameIndex;
        OwnPlayer.PuzzlePieces = (byte)puzzlePieces;
        OwnPlayer.EntitySnapshot = entitySnapshot;
        OwnPlayer.Updated = DateTime.Now;

        var packet = new PlayerStateUpdatePacket(OwnPlayer.PlayerId, OwnPlayer.AccessToken, OwnPlayer.SpeedrunFrameIndex, OwnPlayer.PuzzlePieces, OwnPlayer.EntitySnapshot);
        _udpHelper.SendPacket(packet, _serverEndpoint);

        // Remove stale players (there's probably a more suitable place to do this)
        var stalePlayerIds = _otherPlayers.Values.Where(x => x.Stale).Select(x => x.PlayerId).ToList();
        stalePlayerIds.ForEach(x => _otherPlayers.Remove(x));
    }

    /// <summary>Updates current player state with new frame index (used to send keep-alive packet when game is paused)</summary>
    public void SendPlayerStateUpdate(int frameIndex)
    {
        if (!IsConnected)
            return;

        SendPlayerStateUpdate(OwnPlayer.SpeedrunFrameIndex, OwnPlayer.PuzzlePieces, OwnPlayer.EntitySnapshot with { FrameIndex = frameIndex });
    }

    private void OnPacketReceived(byte[] data, IPEndPoint sender)
    {
        if (data.Length == 0)
            return;

        var packetType = (PacketType)data[0];
        switch (packetType)
        {
            case PacketType.PlayerJoinResponse:
                if (PacketParser.TryParse<PlayerJoinResponsePacket>(data, out var playerJoinResponsePacket))
                    HandlePlayerJoinResponse(playerJoinResponsePacket);
                break;
            case PacketType.PlayerStateBroadcast:
                if (PacketParser.TryParse<PlayerStateBroadcastPacket>(data, out var playerStateBroadcastPacket))
                    HandlePlayerStateBroadcast(playerStateBroadcastPacket);
                break;
            default:
                Console.WriteLine($"Unsupported packet type: {packetType}");
                break;
        }
    }

    private void HandlePlayerJoinResponse(PlayerJoinResponsePacket packet)
    {
        if (!packet.Accepted)
        {
            Console.WriteLine("Connection request refused by server");
            if (packet.ApiVersion != ApiVersion.Current)
                Console.WriteLine($"Version mismatch: Client={ApiVersion.Current}, Server={packet.ApiVersion}");
            return;
        }

        OwnPlayer = new()
        {
            PlayerId = packet.PlayerId,
            AccessToken = packet.AccessToken,
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
                AccessToken = default,
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
}
