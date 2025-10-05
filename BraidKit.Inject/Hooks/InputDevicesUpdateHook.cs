using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "Input_Devices::update" function</summary>
internal class InputDevicesUpdateHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void InputDevicesUpdateDelegate(IntPtr inputDevicesPtr);
    private readonly Action _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly InputDevicesUpdateDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public InputDevicesUpdateHook(Action hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new InputDevicesUpdateDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory adress, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr inputDevicesUpdateAddr = 0x5317b0;
        _jumpHook = JumpHook.Create(inputDevicesUpdateAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<InputDevicesUpdateDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookCallbackFunction(IntPtr inputDevicesPtr)
    {
        _hookAction();
        _originalFunction(inputDevicesPtr);
    }
}
