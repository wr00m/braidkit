using BraidKit.Core.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Network;

public sealed class UdpHelper : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly Thread _thread;
    private readonly Action<byte[], IPEndPoint> _packetReceivedCallback;

    public static bool TryResolveIPAdress(string ipOrDnsHostname, [NotNullWhen(true)] out IPAddress? ipAddress)
    {
        // Not sure which one to choose if more than one, so just pick one at random
        ipAddress = Dns.GetHostAddresses(ipOrDnsHostname).OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
        return ipAddress != null;
    }

    public UdpHelper(int clientPort, Action<byte[], IPEndPoint> packetReceivedCallback)
    {
        _udpClient = new(clientPort);
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
            ConsoleHelper.WriteError(ex.Message);
            return [];
        }
    }

    /// <summary>Prevents Windows from throwing <see cref="SocketError.ConnectionReset"/> on ICMP "Port Unreachable"</summary>
    public static void DisableUdpConnectionReset(Socket socket)
    {
        const int SIO_UDP_CONNRESET = -1744830452;
        socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0], null);
    }
}
