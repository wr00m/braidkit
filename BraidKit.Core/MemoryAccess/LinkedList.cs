namespace BraidKit.Core.MemoryAccess;

/// <summary>Represents an implementation of std::list</summary>
internal class LinkedList<T>(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr) where T : unmanaged
{
    public const int StructSize = sizeof(int) * 3;
    public GameValue<int> ItemCount { get; } = new(_processMemoryHandler, _addr);
    private GameValue<IntPtr> FirstNodeAddr { get; } = new(_processMemoryHandler, _addr + 0x4);
    private LinkedListNode? FirstNode => FirstNodeAddr.Value is IntPtr addr && addr != IntPtr.Zero ? new(_processMemoryHandler, addr) : null;

    public IEnumerable<T> GetItems()
    {
        for (var node = FirstNode; node != null; node = node.NextNode)
            yield return node.Data;
    }

    private class LinkedListNode(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr)
    {
        private GameValue<IntPtr> NextNodeAddr { get; } = new(_processMemoryHandler, _addr);
        public LinkedListNode? NextNode => NextNodeAddr.Value is IntPtr addr && addr != IntPtr.Zero ? new(_processMemoryHandler, addr) : null;
        public GameValue<T> Data { get; } = new(_processMemoryHandler, _addr + 0x8);
    }
}