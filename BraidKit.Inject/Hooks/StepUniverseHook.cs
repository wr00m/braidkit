using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "step_universe" function, which happens every game frame</summary>
internal class StepUniverseHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StepUniverseDelegate(IntPtr entityManagerPtr);
    private readonly Action _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly StepUniverseDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public StepUniverseHook(Action hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new StepUniverseDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr funcAddr = 0x4bb880;
        _jumpHook = JumpHook.Create(funcAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<StepUniverseDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookCallbackFunction(IntPtr entityManagerPtr)
    {
        _hookAction();
        _originalFunction(entityManagerPtr);
    }
}
