using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject;

internal static class Bootstrapper
{
    private static readonly BraidGame _braidGame;
    private static readonly IDirect3DDevice9 _device;
    private static readonly GameRenderer _gameRenderer;
    private static readonly EndSceneHook _endSceneHook;

    static Bootstrapper()
    {
        try
        {
            _braidGame = BraidGame.GetFromCurrentProcess();
            _device = new(_braidGame.DisplaySystem.IDirect3DDevice9Addr);
            _gameRenderer = new(_braidGame, _device);
            _endSceneHook = new(_device, () => _gameRenderer.Render());
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
        // Load argument struct from unmanaged memory
        _gameRenderer.RenderSettings = Marshal.PtrToStructure<RenderSettings>(argsAddr);
        return _gameRenderer.RenderSettings.IsRenderingActive() ? 1 : 0;
    }
}
