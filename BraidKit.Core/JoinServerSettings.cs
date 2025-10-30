using BraidKit.Core.Network;
using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinServerSettings()
{
    public required IntPtr ServerAddress;
    public required int ServerPort;
    public required FixedLengthAsciiString PlayerName;
    public required PlayerColor PlayerColor;
}
