using BraidKit.Core;
using BraidKit.Inject.Rendering;
using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Hooks;

/// <summary>
/// Callback hook for Direct3D's "end scene" function, which happens just before the next frame is rendered
/// </summary>
internal class EndSceneHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EndSceneDelegate(IntPtr devicePtr);
    private readonly Action<IDirect3DDevice9> _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly EndSceneDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public EndSceneHook(BraidGame braidGame, Action<IDirect3DDevice9> hookAction)
    {
        // Get end scene function pointer
        var device = new IDirect3DDevice9(braidGame.DisplaySystem.IDirect3DDevice9Addr.Value);
        var endSceneAddr = device.GetEndSceneAddr();

        // Setup hook/trampoline
        var del = new EndSceneDelegate(HookedEndScene);
        _gcHandle = GCHandle.Alloc(del); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(del);
        _hookAction = hookAction;
        _jumpHook = JumpHook.Create(endSceneAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<EndSceneDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private int HookedEndScene(IntPtr devicePtr)
    {
        var device = new IDirect3DDevice9(devicePtr);
        _hookAction(device);
        var result = _originalFunction(devicePtr);
        return result;
    }
}
