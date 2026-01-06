using System.Runtime.InteropServices;

namespace BraidKit.Core.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinServerSettings()
{
    public required IntPtr ServerAddress;
    public required int ServerPort;
    public required IntPtr PlayerName;
    public required PlayerColor PlayerColor;

    public static JoinServerSettings Disconnect => new()
    {
        ServerAddress = IntPtr.Zero,
        ServerPort = default,
        PlayerName = IntPtr.Zero,
        PlayerColor = default,
    };
}
