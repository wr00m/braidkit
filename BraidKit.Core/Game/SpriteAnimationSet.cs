using System.Numerics;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Game;

[StructLayout(LayoutKind.Explicit)]
public readonly struct SpriteAnimationSet
{
    [FieldOffset(0x0)] public readonly IntPtr Name;
    [FieldOffset(0x4)] public readonly IntPtr TextureMap;
    [FieldOffset(0x8)] public readonly IntPtr TextureName;
    [FieldOffset(0xc)] public readonly int NumAnimations;
    [FieldOffset(0x10)] public readonly IntPtr AnimationArray;
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct SpriteAnimation
{
    [FieldOffset(0x0)] public readonly IntPtr Name;
    [FieldOffset(0x4)] public readonly float Duration;
    [FieldOffset(0x8)] public readonly int Flags;
    [FieldOffset(0xc)] public readonly int NumFrames;
    [FieldOffset(0x10)] public readonly IntPtr FrameArray;
    [FieldOffset(0x14)] public readonly float YOfGroundLevel;
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct SpriteAnimationFrame
{
    [FieldOffset(0x0)] public readonly int FrameFlags;
    [FieldOffset(0x4)] public readonly int Width;
    [FieldOffset(0x8)] public readonly int Height;
    [FieldOffset(0xc)] public readonly int X0;
    [FieldOffset(0x10)] public readonly int Y0;
    [FieldOffset(0x14)] public readonly Vector2 Offset;
    [FieldOffset(0x1c)] public readonly Vector2 OriginOffset;
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct TextureMap
{
    [FieldOffset(0x0)] public readonly IntPtr Name;
    [FieldOffset(0x4)] public readonly ushort Width;
    [FieldOffset(0x6)] public readonly ushort Height;
    [FieldOffset(0x8)] public readonly int TextureFormat;
    [FieldOffset(0xc)] public readonly int Flags;
    [FieldOffset(0x10)] public readonly IntPtr BitmapData;
    [FieldOffset(0x14)] public readonly uint FrameLastUsed;
}
