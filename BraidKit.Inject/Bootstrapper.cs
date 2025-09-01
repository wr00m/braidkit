using BraidKit.Core.Game;
using BraidKit.Inject.Hooks;
using BraidKit.Inject.Rendering;

namespace BraidKit.Inject;

internal static class Bootstrapper
{
    private static bool _showHitboxes = false;
    private static BraidGame? _braidGame;
    private static Renderer? _renderer;
    private static EndSceneHook? _endSceneHook;

    [STAThread]
    public static int ToggleHitboxes(IntPtr argsAddr, int size)
    {
        _showHitboxes = !_showHitboxes;
        _braidGame ??= BraidGame.GetFromCurrentProcess();
        _renderer ??= new(_braidGame);
        _endSceneHook ??= new(_braidGame, device =>
        {
            if (_showHitboxes)
                _renderer.RenderCollisionGeometries(device);
        });
        return _showHitboxes ? 1 : 0;
    }
}
