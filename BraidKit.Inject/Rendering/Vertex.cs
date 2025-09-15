using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal interface IVertex
{
    static abstract VertexFormat Format { get; }
    static abstract uint Size { get; }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct LineVertex(float x, float y, float nx, float ny, float blend) : IVertex
{
    public readonly Vector3 Position = new(x, y, 0f);
    public readonly Vector3 Normal = new(nx, ny, 0f);
    public readonly float Texcoord = blend;

    public static VertexFormat Format => VertexFormat.Position | VertexFormat.Normal | VertexFormat.Texture1;
    public static uint Size => (uint)Marshal.SizeOf<LineVertex>();
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct FontVertex(float x, float y, float u, float v) : IVertex
{
    public readonly Vector3 Position = new(x, y, 0f);
    public readonly Vector2 Texcoord = new(u, v);

    public static VertexFormat Format => VertexFormat.Position | VertexFormat.Texture1;
    public static uint Size => (uint)Marshal.SizeOf<FontVertex>();
}
