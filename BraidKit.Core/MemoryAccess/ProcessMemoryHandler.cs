using System.Runtime.InteropServices;

namespace BraidKit.Core.MemoryAccess;

public class ProcessMemoryHandler : IDisposable
{
    private readonly IntPtr _hProc;

    public ProcessMemoryHandler(int processId)
    {
        _hProc = OpenProcess(ProcessAccessFlags.All, false, processId);
    }

    public void Dispose()
    {
        if (_hProc != default)
            CloseHandle(_hProc);
    }

    // TODO: Read/write memory with unsafe code if running within the braid.exe process (should be faster)
    public T Read<T>(IntPtr addr) where T : unmanaged => FromBytes<T>(ReadBytes(addr, GetBlittableSize<T>()));
    public void Write<T>(IntPtr addr, T val) where T : unmanaged => WriteBytes(addr, ToBytes(val));
    public byte[] ReadBytes(IntPtr addr, int count) { var buff = new byte[count]; ReadProcessMemory(_hProc, addr, buff, count, out _); return buff; }
    public void WriteBytes(IntPtr addr, byte[] bytes) => WriteProcessMemory(_hProc, addr, bytes, (uint)bytes.Length, out var _);
    public void CallFunction(IntPtr addr) => CreateRemoteThread(_hProc, IntPtr.Zero, 0, addr, IntPtr.Zero, 0, out _);

    public string ReadNullTerminatedString(IntPtr addr, bool isPointer = true)
    {
        if (isPointer)
            addr = Read<int>(addr);
        var result = "";
        for (var i = 0; true; i++)
        {
            var c = (char)Read<byte>(addr + i);
            if (c == '\0')
                break;
            result += c;
        }
        return result;
    }

    private static byte[] ToBytes<T>(T value) where T : unmanaged
    {
        var bytes = new byte[GetBlittableSize<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
    }

    private static T FromBytes<T>(byte[] bytes) where T : unmanaged
    {
        return MemoryMarshal.Read<T>(bytes);
    }

    public static int GetBlittableSize<T>() => Marshal.SizeOf(GetBlittableType<T>());
    private static Type GetBlittableType<T>() => typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);

    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);
    [DllImport("kernel32.dll")] static extern int CloseHandle(IntPtr hProcess);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);
    private enum ProcessAccessFlags : uint { All = 0x001f0fff }
}
