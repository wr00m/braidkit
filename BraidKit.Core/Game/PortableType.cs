using BraidKit.Core.MemoryAccess;

namespace BraidKit.Core.Game;

public class PortableType(ProcessMemoryHandler _processMemoryHandler, PortableTypeAddr _addr)
{
    /// <summary>Used as identifier and array index</summary>
    public GameValue<int> SerialNumber { get; } = new(_processMemoryHandler, (IntPtr)_addr + 0x8);

    public override string ToString() => _addr.ToString();
}

/// <summary>Hex values are portable type instance addresses</summary>
public enum PortableTypeAddr
{
    Guy = 0x5f6840,
    Greeter = 0x5f6810,
    Monstar = 0x57c090,
    PuzzleFrame = 0x63f558,
}