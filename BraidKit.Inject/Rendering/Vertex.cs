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
internal readonly struct LineVertex(float x, float y, float z, float nx, float ny, float nz) : IVertex
{
    public readonly Vector3 Position = new(x, y, z);
    public readonly Vector3 Normal = new(nx, ny, nz);

    public static VertexFormat Format => VertexFormat.Position | VertexFormat.Normal;
    public static uint Size => (uint)Marshal.SizeOf<LineVertex>();
}
