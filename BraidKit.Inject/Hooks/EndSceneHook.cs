using BraidKit.Inject.Rendering;
using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for Direct3D's "end scene" function, which happens just before the next frame is rendered</summary>
internal class EndSceneHook : IDisposable
{
    private readonly IDirect3DDevice9 _device;
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EndSceneDelegate(IntPtr devicePtr);
    private readonly Action _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly EndSceneDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public EndSceneHook(IDirect3DDevice9 device, Action hookAction)
    {
        // Get end scene function pointer
        _device = device;
        var endSceneAddr = _device.GetEndSceneAddr();

        // Setup hook/trampoline
        var @delegate = new EndSceneDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        _jumpHook = JumpHook.Create(endSceneAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<EndSceneDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private int HookCallbackFunction(IntPtr devicePtr)
    {
        // This check is probably unnecessary...
        if (devicePtr == _device.NativePointer)
            _hookAction();

        var result = _originalFunction(devicePtr);
        return result;
    }
}
