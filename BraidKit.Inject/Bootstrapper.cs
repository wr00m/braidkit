using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
using BraidKit.Network;
using InjectDotnet.NativeHelper.Native;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject;

internal static class Bootstrapper
{
    private static readonly BraidGame _braidGame;
    private static readonly IDirect3DDevice9 _device;
    private static readonly GameRenderer _gameRenderer;
    private static readonly EndSceneHook _endSceneHook;
    private static readonly GetGuyAnimationIndexAndDurationHook _getGuyAnimationIndexAndDurationHook;
    private static readonly StopRenderingEntitiesHook _stopRenderingEntitiesHook;
    private static Client? _multiplayerClient;

    [MemberNotNullWhen(true, nameof(_multiplayerClient))]
    private static bool IsConnected => _multiplayerClient?.IsConnected == true;

    static Bootstrapper()
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            _braidGame = BraidGame.GetFromCurrentProcess();
            _device = new(_braidGame.DisplaySystem.IDirect3DDevice9Addr);
            _gameRenderer = new(_braidGame, _device);
            _endSceneHook = new(_device, () =>
            {
                _gameRenderer.Render();

                // Render other player names
                if (IsConnected)
                    _gameRenderer.RenderPlayerLabelsAndLeaderboard(_multiplayerClient.GetPlayers());
            });
            _getGuyAnimationIndexAndDurationHook = new((entityAddr, animationIndex, animationTime) =>
            {
                // Send Tim's position to server
                if (IsConnected && _braidGame.TryGetTim(out var tim) && tim.Addr == entityAddr)
                {
                    // This hook triggers more than once per frame, but client won't send duplicates to server
                    var puzzlePieces = _braidGame.CountAcquiredPuzzlePieces();
                    var snapshot = new EntitySnapshot(_braidGame.FrameCount, (byte)_braidGame.TimWorld, (byte)_braidGame.TimLevel, tim.Position, tim.FacingLeft, (byte)animationIndex, animationTime);
                    _multiplayerClient.SendPlayerStateUpdate(_braidGame.SpeedrunFrameIndex, puzzlePieces, snapshot);
                }
            });
            _stopRenderingEntitiesHook = new(() =>
            {
                // Render other players
                if (IsConnected)
                    foreach (var player in _multiplayerClient.GetPlayers().Where(x => !x.IsOwnPlayer).Select(x => x.EntitySnapshot))
                        if (player.World == _braidGame.TimWorld && player.Level == _braidGame.TimLevel && _braidGame.TryCreateTimGameQuad(player, out var gameQuad, 0x80ffffff))
                            _braidGame.AddGameQuad(gameQuad);
            });
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }

    [STAThread]
    public static int Render(IntPtr argsAddr, int _)
    {
        try
        {
            // Load argument struct from unmanaged memory
            _gameRenderer.RenderSettings = Marshal.PtrToStructure<RenderSettings>(argsAddr);
            return _gameRenderer.RenderSettings.IsRenderingActive() ? 1 : 0;
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }

    [STAThread]
    public unsafe static int JoinServer(IntPtr argsAddr, int _)
    {
        try
        {
            // Load argument struct from unmanaged memory
            var args = Marshal.PtrToStructure<JoinServerSettings>(argsAddr);

            if (_multiplayerClient != null)
            {
                _multiplayerClient.Dispose();
                _multiplayerClient = null;
            }

            if (args.ServerAddress == IntPtr.Zero)
                return 0;

            var serverAddress = new string(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)args.ServerAddress));
            NativeMethods.VirtualFree(args.ServerAddress, 0, FreeType.Release);

            //Logger.Log($"Server address: {serverAddress}");

            if (!UdpHelper.TryResolveIPAdress(serverAddress, out var serverIP))
            {
                Logger.Log($"Invalid server IP address or hostname: {serverAddress}");
                return 0;
            }

            //Logger.Log($"Server IP: {serverIP}");

            // TODO: Get player name from Steam?
            _multiplayerClient = new(serverIP, args.ServerPort);
            var connected = _multiplayerClient.ConnectToServer(args.PlayerName).Result;

            return connected ? 1 : 0;
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }
}
