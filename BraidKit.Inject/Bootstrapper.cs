using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
using BraidKit.Network;
using InjectDotnet.NativeHelper.Native;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;
using Vortice.Mathematics;

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

                if (IsConnected)
                {
                    _multiplayerClient.SendPlayerStateUpdate(_braidGame.FrameCount);
                    _gameRenderer.RenderPlayerLabelsAndLeaderboard(_multiplayerClient.GetPlayers());
                }
            });
            _getGuyAnimationIndexAndDurationHook = new((entityAddr, animationIndex, animationTime) =>
            {
                // Note: This hook triggers more than once per frame
                if (IsConnected && _braidGame.TryGetTim(out var tim) && tim.Addr == entityAddr)
                {
                    var puzzlePieces = _braidGame.CountAcquiredPuzzlePieces();
                    var snapshot = new EntitySnapshot(_braidGame.FrameCount, (byte)_braidGame.TimWorld, (byte)_braidGame.TimLevel, tim.Position, tim.FacingLeft, (byte)animationIndex, animationTime);
                    _multiplayerClient.SendPlayerStateUpdate(_braidGame.SpeedrunFrameIndex, puzzlePieces, snapshot);
                }
            });
            _stopRenderingEntitiesHook = new(() =>
            {
                // Render other players
                // Note: Checking which entity manager is active (hopefully) prevents random Tim clones that otherwise appear on screen sometimes
                if (IsConnected && _braidGame.IsUsualEntityManagerActive())
                {
                    var playersToRender = _multiplayerClient
                        .GetPlayers()
                        .Where(x => !x.IsOwnPlayer && x.EntitySnapshot.World == _braidGame.TimWorld && x.EntitySnapshot.Level == _braidGame.TimLevel)
                        .ToList();

                    if (playersToRender.Count > 0)
                    {
                        var playerColor = new Vector4(1f, 1f, 1f, .5f); // Semi-transparent
                        var fadedColor = new Color4(playerColor * _braidGame.EntityVertexColorScale);

                        foreach (var player in playersToRender)
                            if (_braidGame.TryCreateTimGameQuad(player.EntitySnapshot, out var gameQuad, fadedColor.ToRgba()))
                                _braidGame.AddGameQuad(gameQuad);
                    }
                }
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

            if (!UdpHelper.TryResolveIPAddress(serverAddress, out var serverIP))
            {
                Logger.Log($"Invalid server IP address or hostname: {serverAddress}");
                return 0;
            }

            // TODO: Get player name from Steam?
            _multiplayerClient = new(serverIP, args.ServerPort);
            var connected = _multiplayerClient.ConnectToServer(args.PlayerName, args.PlayerColor).Result;

            return connected ? 1 : 0;
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }
}
