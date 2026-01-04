using System.Runtime.InteropServices;
using System.Text;

namespace BraidKit.Inject;

internal class KeyboardState
{
    private const int VirtualKeyCount = 256;
    private readonly byte[] _currVirtualKeyStates = new byte[VirtualKeyCount];
    private readonly byte[] _prevVirtualKeyStates = new byte[VirtualKeyCount];

    /// <summary>Call this once per tick to update key states</summary>
    public KeyboardState UpdateState()
    {
        Buffer.BlockCopy(_currVirtualKeyStates, 0, _prevVirtualKeyStates, 0, VirtualKeyCount);
        GetKeyboardState(_currVirtualKeyStates);
        return this;
    }

    public bool TryGetTypedChars(out string chars)
    {
        chars = "";
        foreach (var vk in GetJustPressedKeys())
        {
            uint scanCode = MapVirtualKey((uint)vk, MAPVK_VK_TO_VSC);
            var sb = new StringBuilder(2);
            int success = ToUnicodeEx((uint)vk, scanCode, _currVirtualKeyStates, sb, sb.Capacity, 0, IntPtr.Zero);
            if (success > 0)
                chars += sb[0];
        }
        return chars.Length > 0;
    }

    public List<VirtualKey> GetJustPressedKeys()
    {
        var result = new List<VirtualKey>();
        for (int i = 0; i < VirtualKeyCount; i++)
            if (WasKeyJustPressed((VirtualKey)i))
                result.Add((VirtualKey)i);
        return result;
    }

    public bool WasKeyJustPressed(VirtualKey key)
    {
        var i = (int)key;
        if (i >= VirtualKeyCount)
            return false;
        var wasPressed = (_prevVirtualKeyStates[i] & KEY_PRESSED) != 0;
        var isPressed = (_currVirtualKeyStates[i] & KEY_PRESSED) != 0;
        return !wasPressed && isPressed;
    }

    private const byte KEY_PRESSED = 0x80;
    private const uint MAPVK_VK_TO_VSC = 0x01;
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    [DllImport("user32.dll")] private static extern bool GetKeyboardState(byte[] lpKeyState);
    [DllImport("user32.dll")] private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);
}

internal enum VirtualKey : byte
{
    Back = 0x08,
    Enter = 0x0d,
}
