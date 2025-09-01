using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal static class Direct3DExtensions
{
    public static unsafe IntPtr GetEndSceneAddr(this IDirect3DDevice9 device)
    {
        var vtable = *(IntPtr**)device.NativePointer;
        var endScenePtr = vtable[42];
        return endScenePtr;
    }
}
