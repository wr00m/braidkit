using BraidKit.Core.MemoryAccess;

namespace BraidKit.Core.Game;

public class DisplaySystem(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr)
{
    public GameValue<int> IDirect3DDevice9Addr { get; } = new(_processMemoryHandler, _addr + 0xe4);
}
