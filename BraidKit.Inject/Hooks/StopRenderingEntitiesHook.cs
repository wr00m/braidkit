using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "stop_rendering_entities" function</summary>
internal class StopRenderingEntitiesHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StopRenderingEntitiesDelegate();
    private readonly Action _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly StopRenderingEntitiesDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public StopRenderingEntitiesHook(Action hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new StopRenderingEntitiesDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr functionAddr = 0x4f8160;
        _jumpHook = JumpHook.Create(functionAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<StopRenderingEntitiesDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookCallbackFunction()
    {
        _hookAction();
        _originalFunction();
    }
}
