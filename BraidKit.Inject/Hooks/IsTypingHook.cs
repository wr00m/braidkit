using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "is_typing" function, which returns true if a visual text field is currently capturing keyboard input</summary>
internal class IsTypingHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool IsTypingDelegate();
    private readonly Func<bool?> _hookAction; // If not null, used instead of return value from original function
    private readonly JumpHook _jumpHook;
    private readonly IsTypingDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public IsTypingHook(Func<bool?> hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new IsTypingDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory address, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr functionAddr = 0x4919c0;
        _jumpHook = JumpHook.Create(functionAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<IsTypingDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private bool HookCallbackFunction()
    {
        return _hookAction() ?? _originalFunction();
    }
}
