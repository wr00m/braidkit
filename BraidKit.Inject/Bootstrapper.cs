using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject;

internal static class Bootstrapper
{
    private static bool _renderColliders = false; // TODO: Move to GameRenderer, probably
    private static readonly BraidGame _braidGame;
    private static readonly IDirect3DDevice9 _device;
    private static readonly GameRenderer _gameRenderer;
    private static readonly EndSceneHook _endSceneHook;

    static Bootstrapper()
    {
        _braidGame = BraidGame.GetFromCurrentProcess();
        _device = new(_braidGame.DisplaySystem.IDirect3DDevice9Addr);
        _gameRenderer = new(_braidGame, _device);
        _endSceneHook = new(_device, () =>
        {
            if (_renderColliders && !_braidGame.InMainMenu && !_braidGame.InPuzzleAssemblyScreen)
                _gameRenderer.RenderCollisionGeometries();
        });
    }

    [STAThread]
    public static int Render(IntPtr argsAddr, int _)
    {
        // Load argument struct from unmanaged memory
        var renderSettings = Marshal.PtrToStructure<RenderSettings>(argsAddr);
        _renderColliders = renderSettings.RenderColliders;
        _gameRenderer.LineWidth = renderSettings.LineWidth;

        return _renderColliders ? 1 : 0;
    }
}
