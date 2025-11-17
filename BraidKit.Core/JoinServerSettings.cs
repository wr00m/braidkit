using BraidKit.Network;
using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinServerSettings()
{
    public required IntPtr ServerAddress;
    public required int ServerPort;
    public required FixedLengthAsciiString PlayerName;
    public required PlayerColor PlayerColor;

    public static JoinServerSettings Disconnect => new()
    {
        ServerAddress = IntPtr.Zero,
        ServerPort = default,
        PlayerName = default,
        PlayerColor = default,
    };
}
