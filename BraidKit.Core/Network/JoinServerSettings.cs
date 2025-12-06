using System.Runtime.InteropServices;

namespace BraidKit.Core.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinServerSettings()
{
    public required nint ServerAddress;
    public required int ServerPort;
    public required FixedLengthAsciiString PlayerName;
    public required PlayerColor PlayerColor;

    public static JoinServerSettings Disconnect => new()
    {
        ServerAddress = nint.Zero,
        ServerPort = default,
        PlayerName = default,
        PlayerColor = default,
    };
}
