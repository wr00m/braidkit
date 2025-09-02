using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;
using System.Runtime.InteropServices;

namespace BraidKit.Inject;

internal static class Bootstrapper
{
    private static bool _renderColliders = false;
    private static BraidGame? _braidGame;
    private static Renderer? _renderer;
    private static EndSceneHook? _endSceneHook;

    [STAThread]
    public static int Render(IntPtr argsAddr, int size)
    {
        // Load argument struct from unmanaged memory
        var renderSettings = Marshal.PtrToStructure<RenderSettings>(argsAddr);

        _renderColliders = renderSettings.RenderColliders;
        _braidGame ??= BraidGame.GetFromCurrentProcess();
        _renderer ??= new(_braidGame);
        _endSceneHook ??= new(_braidGame, device =>
        {
            if (_renderColliders)
                _renderer.RenderCollisionGeometries(device);
        });

        _renderer.LineWidth = renderSettings.LineWidth;

        return _renderColliders ? 1 : 0;
    }
}
