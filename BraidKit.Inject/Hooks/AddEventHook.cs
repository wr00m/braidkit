using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "Event_Manager::add_event" function, which for example triggers when a key is pressed</summary>
internal class AddEventHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void AddEventDelegate(IntPtr eventManagerPtr, IntPtr eventPtr);
    private readonly Func<bool> _hookAction; // Return value decides whether original function should be called
    private readonly JumpHook _jumpHook;
    private readonly AddEventDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public AddEventHook(Func<bool> hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new AddEventDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory address, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr functionAddr = 0x520a30;
        _jumpHook = JumpHook.Create(functionAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<AddEventDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookCallbackFunction(IntPtr eventManagerPtr, IntPtr eventPtr)
    {
        if (_hookAction())
            _originalFunction(eventManagerPtr, eventPtr);
    }
}
