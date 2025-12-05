using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BraidKit.Network;

public sealed class UdpHelper : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly Thread _thread;
    private readonly Action<byte[], IPEndPoint> _packetReceivedCallback;

    public static async Task<IPAddress?> ResolveIPAddress(string ipOrDnsHostname, int maxAttempts = 10, int retryWaitMs = 500)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var ipAddresses = await Dns.GetHostAddressesAsync(ipOrDnsHostname);

                // Prefer IPv4, fallback to IPv6
                var ipAddress = ipAddresses
                    .OrderByDescending(x => x.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault();

                return ipAddress;
            }
            catch (SocketException ex) when (IsTransientError(ex.SocketErrorCode))
            {
                // Retry if transient error and not last attempt
                if (i < maxAttempts - 1)
                    await Task.Delay(retryWaitMs);
            }
            catch (SocketException ex)
            {
                // Give up if non-transient error
                await Console.Error.WriteLineAsync($"Socket error: {ex.SocketErrorCode}");
                return null;
            }
        }

        return null;
    }

    public UdpHelper(int clientPort, Action<byte[], IPEndPoint> packetReceivedCallback)
    {
        _udpClient = new(clientPort);

        if (OperatingSystem.IsWindows())
            DisableUdpConnectionReset(_udpClient.Client);

        _thread = new(ReceiveLoop);
        _thread.Start();
        _packetReceivedCallback = packetReceivedCallback;
    }

    public void Dispose()
    {
        _udpClient.Close();
        _udpClient.Dispose();
        _thread.Join();
    }

    private void ReceiveLoop()
    {
        while (true)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpClient.Receive(ref endpoint);
                _packetReceivedCallback(data, endpoint);
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionReset)
            {
                // This happens when a client disconnects; we can safely ignore this
            }
            catch (SocketException)
            {
                // TODO: Not sure what to do here
                break;
            }
        }
    }

    public void SendPacket<T>(T packet, IPEndPoint endpoint) where T : unmanaged
    {
        var bytes = StructToBytes(packet);

        if (bytes.Length > 0)
            _udpClient.Send(bytes, bytes.Length, endpoint);
    }

    public unsafe static byte[] StructToBytes<T>(T @struct) where T : unmanaged
    {
        try
        {
            var bytes = new byte[sizeof(T)];
            var span = bytes.AsSpan();
            MemoryMarshal.Write(span, in @struct);
            return bytes;
        }
        catch (Exception ex)
        {
            Console.Error.Write(ex.Message);
            return [];
        }
    }

    /// <summary>Prevents Windows from throwing <see cref="SocketError.ConnectionReset"/> on ICMP "Port Unreachable"</summary>
    [SupportedOSPlatform("windows")]
    public static void DisableUdpConnectionReset(Socket socket)
    {
        const int SIO_UDP_CONNRESET = -1744830452;
        socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0], null);
    }

    private static bool IsTransientError(SocketError error) => error
        is SocketError.TimedOut
        or SocketError.HostNotFound
        or SocketError.TryAgain
        or SocketError.NetworkUnreachable
        or SocketError.HostUnreachable
        or SocketError.ConnectionReset
        or SocketError.ConnectionAborted
        or SocketError.ConnectionRefused;
}
