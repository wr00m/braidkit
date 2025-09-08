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
        var renderSettings = Marshal.PtrToStructure<RenderSettings>(argsAddr);

        _gameRenderer.RenderColliders = renderSettings.RenderColliders;
        _gameRenderer.RenderVelocity = renderSettings.RenderVelocity;
        _gameRenderer.LineWidth = renderSettings.LineWidth;
        _gameRenderer.FontSize = renderSettings.FontSize;
        _gameRenderer.FontColor = new(renderSettings.FontColor);

        return _gameRenderer.IsRenderingActive ? 1 : 0;
    }
}
