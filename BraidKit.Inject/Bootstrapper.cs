using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using BraidKit.Core.Network;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
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
    private static readonly AddEventHook _addEventHook;
    private static readonly ChatInput _chatInput = new();
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
                    _gameRenderer.RenderPlayerLabelsAndLeaderboard(_multiplayerClient.GetPlayers(), _multiplayerClient.GetChatLog());

                    if (!_braidGame.InMainMenu)
                    {
                        if (_chatInput.Update(out var completedMessage))
                            _multiplayerClient.SendChatMessage(completedMessage);

                        _gameRenderer.RenderChat(_multiplayerClient.GetChatLog(), _chatInput.IsActive, _chatInput.Message, _multiplayerClient.GetOwnPlayerColor());
                    }
                }
            });
            _getGuyAnimationIndexAndDurationHook = new((entityAddr, animationIndex, animationTime) =>
            {
                // Note: This hook triggers more than once per frame
                if (IsConnected && _braidGame.TryGetTim(out var tim) && tim.Addr == entityAddr)
                {
                    var puzzlePieces = _braidGame.CurrentCampaignState.CountAcquiredPuzzlePieces();
                    var snapshot = new EntitySnapshot(_braidGame.FrameCount, (byte)_braidGame.TimWorld, (byte)_braidGame.TimLevel, tim.Position, tim.FacingLeft, (byte)animationIndex, animationTime);
                    _multiplayerClient.SendPlayerStateUpdate(_braidGame.SpeedrunFrameIndex, puzzlePieces, snapshot);
                }
            });
            _stopRenderingEntitiesHook = new(() =>
            {
                if (IsConnected)
                {
                    // Poll for updates
                    _multiplayerClient.PollEvents();

                    // Note: Checking which entity manager is active (hopefully) prevents random Tim clones that otherwise appear on screen sometimes
                    if (_braidGame.IsUsualEntityManagerActive())
                    {
                        var playersToRender = _multiplayerClient
                            .GetPlayers()
                            .Where(x => !x.IsOwnPlayer && x.EntitySnapshot.World == _braidGame.TimWorld && x.EntitySnapshot.Level == _braidGame.TimLevel)
                            .ToList();

                        // Render other players
                        if (playersToRender.Count > 0)
                        {
                            var playerColor = new Vector4(1f, 1f, 1f, .5f); // Semi-transparent
                            var fadedColor = new Color4(playerColor * _braidGame.EntityVertexColorScale).ToColor();

                            foreach (var player in playersToRender)
                                if (_braidGame.TryCreateTimGameQuad(player.EntitySnapshot, out var gameQuad, fadedColor))
                                    _braidGame.AddGameQuad(gameQuad);
                        }
                    }
                }
            });
            _addEventHook = new(() =>
            {
                // Prevent game from adding keyboard events when chat input is active
                return !_chatInput.IsActive;
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
    public static int JoinServer(IntPtr argsAddr, int _)
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

            var serverAddress = ReadAndReleaseStringArg(args.ServerAddress);
            var playerName = ReadAndReleaseStringArg(args.PlayerName) ?? "";

            if (string.IsNullOrWhiteSpace(serverAddress))
                return 0;

            _multiplayerClient = new();
            _multiplayerClient.StartSpeedrunEvent += () =>
            {
                if (!_braidGame.IsSpeedrunModeActive)
                    _braidGame.LaunchFullGameSpeedrun();
            };

            var connected = _multiplayerClient.ConnectToServer(serverAddress, args.ServerPort, playerName, args.PlayerColor).GetAwaiter().GetResult();

            return connected ? 1 : 0;
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            throw;
        }
    }

    private static string? ReadAndReleaseStringArg(IntPtr addr)
    {
        if (addr == IntPtr.Zero)
            return null;

        var result = Marshal.PtrToStringUni(addr);
        NativeMethods.VirtualFree(addr, 0, FreeType.Release);

        return result;
    }
}
