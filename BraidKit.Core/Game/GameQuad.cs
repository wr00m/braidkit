using System.Numerics;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Game;

/// <summary>Visual representation of an entity ready for rendering</summary>
[StructLayout(LayoutKind.Explicit)]
public record struct GameQuad
{
    [FieldOffset(0x0)] public float RenderPriority;
    [FieldOffset(0x4)] public float Parallax;
    [FieldOffset(0x8)] public int PortableId;
    [FieldOffset(0xc)] public Vector3 Pos0;
    [FieldOffset(0x18)] public Vector3 Pos1;
    [FieldOffset(0x24)] public Vector3 Pos2;
    [FieldOffset(0x30)] public Vector3 Pos3;
    [FieldOffset(0x3c)] public Vector2 UV0;
    [FieldOffset(0x44)] public Vector2 UV1;
    [FieldOffset(0x4c)] public Vector2 UV2;
    [FieldOffset(0x54)] public Vector2 UV3;
    [FieldOffset(0x5c)] public IntPtr TextureMap;
    [FieldOffset(0x60)] public IntPtr PiecedImage;
    [FieldOffset(0x64)] public int Flags;
    [FieldOffset(0x68)] public uint ScaleColor;
    [FieldOffset(0x6c)] public uint AddColor;
    [FieldOffset(0x70)] public float CompandScale;
}
