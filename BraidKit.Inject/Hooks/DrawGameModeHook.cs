using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>
/// Callback hook for the "draw game mode" function, which happens every game frame (except menu, puzzle screens, etc.)
/// </summary>
internal class DrawGameModeHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void DrawGameModeDelegate();
    private readonly Action _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly DrawGameModeDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public DrawGameModeHook(Action hookAction)
    {
        // Setup hook/trampoline
        var del = new DrawGameModeDelegate(HookedDrawGameMode);
        _gcHandle = GCHandle.Alloc(del); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(del);
        _hookAction = hookAction;
        const IntPtr stepUniverseAddr = 0x4b52c0;
        _jumpHook = JumpHook.Create(stepUniverseAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<DrawGameModeDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookedDrawGameMode()
    {
        _hookAction();
        _originalFunction();
    }
}
