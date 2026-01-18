using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "Keyboard::get_button_state" function</summary>
internal class KeyboardGetButtonStateHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate bool KeyboardGetButtonStateDelegate(IntPtr keyboardPtr, int keyIndex);
    private readonly Func<bool?> _hookAction; // If not null, used instead of return value from original function
    private readonly JumpHook _jumpHook;
    private readonly KeyboardGetButtonStateDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public KeyboardGetButtonStateHook(Func<bool?> hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new KeyboardGetButtonStateDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory address, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr inputDevicesUpdateAddr = 0x531780;
        _jumpHook = JumpHook.Create(inputDevicesUpdateAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<KeyboardGetButtonStateDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private bool HookCallbackFunction(IntPtr keyboardPtr, int keyIndex)
    {
        return _hookAction() ?? _originalFunction(keyboardPtr, keyIndex);
    }
}
