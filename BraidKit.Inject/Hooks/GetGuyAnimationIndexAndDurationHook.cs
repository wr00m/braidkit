using InjectDotnet.NativeHelper;
using System.Runtime.InteropServices;

namespace BraidKit.Inject.Hooks;

/// <summary>Callback hook for the "get_guy_animation_index_and_duration" function</summary>
internal class GetGuyAnimationIndexAndDurationHook : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GetGuyAnimationIndexAndDurationDelegate(IntPtr entity, IntPtr animationIndex, IntPtr animationTime, IntPtr animationDuration);
    private readonly Action<IntPtr, int, float> _hookAction;
    private readonly JumpHook _jumpHook;
    private readonly GetGuyAnimationIndexAndDurationDelegate _originalFunction;
    private readonly GCHandle _gcHandle;

    public GetGuyAnimationIndexAndDurationHook(Action<IntPtr, int, float> hookAction)
    {
        // Setup hook/trampoline
        var @delegate = new GetGuyAnimationIndexAndDurationDelegate(HookCallbackFunction);
        _gcHandle = GCHandle.Alloc(@delegate); // Pin memory address, or stuff will break during garbage collection
        var hookFuncPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
        _hookAction = hookAction;
        const IntPtr functionAddr = 0x4f4900;
        _jumpHook = JumpHook.Create(functionAddr, hookFuncPtr) ?? throw new Exception("Failed to create hook");
        _originalFunction = Marshal.GetDelegateForFunctionPointer<GetGuyAnimationIndexAndDurationDelegate>(_jumpHook.OriginalFunction);
    }

    public void Dispose()
    {
        _jumpHook.Dispose();
        _gcHandle.Free();
    }

    private void HookCallbackFunction(IntPtr entity, IntPtr animationIndex, IntPtr animationTime, IntPtr animationDuration)
    {
        _originalFunction(entity, animationIndex, animationTime, animationDuration);

        unsafe
        {
            _hookAction(entity, *(int*)animationIndex.ToPointer(), *(float*)animationTime.ToPointer());
        }
    }
}
