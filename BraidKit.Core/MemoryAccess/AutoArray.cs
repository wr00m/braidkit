using System.Runtime.InteropServices;

namespace BraidKit.Core.MemoryAccess;

internal class AutoArray<T>(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr) where T : unmanaged
{
    public int GetItemCount() => _processMemoryHandler.Read<int>(_addr);
    private IntPtr GetFirstItemAddr() => _processMemoryHandler.Read<int>(_addr + sizeof(int) * 2);
    private int GetItemSize() => ProcessMemoryHandler.GetBlittableSize<T>();

    public T[] GetAllItems()
    {
        var itemCount = GetItemCount();
        var firstItemAddr = GetFirstItemAddr();
        var itemSize = GetItemSize();
        var bytes = _processMemoryHandler.ReadBytes(firstItemAddr, itemSize * itemCount);
        var result = MemoryMarshal.Cast<byte, T>(bytes);
        return result.ToArray();
    }
}
