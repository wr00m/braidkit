using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal struct Vertex
{
    public readonly Vector3 Position;
    public readonly Vector3 Normal;

    public Vertex(float x, float y, float z, float nx, float ny, float nz)
    {
        Position = new(x, y, z);
        Normal = new(nx, ny, nz);
    }

    public static readonly VertexFormat Format = VertexFormat.Position | VertexFormat.Normal;
    public static uint Size => (uint)Marshal.SizeOf<Vertex>();
}
