using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal class TriangleStrip : IDisposable
{
    public readonly uint PrimitiveCount;
    public uint Stride => Vertex.Size;
    public VertexFormat VertexFormat => Vertex.Format;
    public IDirect3DVertexBuffer9 _vertexBuffer;

    public TriangleStrip(IDirect3DDevice9 device, List<Vertex> verts)
    {
        PrimitiveCount = (uint)verts.Count - 2;
        _vertexBuffer = device.CreateVertexBuffer(
            (uint)verts.Count * Stride,
            Usage.WriteOnly,
            Vertex.Format,
            Pool.Managed);

        var buff = _vertexBuffer.Lock<Vertex>(0, 0, LockFlags.None);
        verts.CopyTo(buff);
        _vertexBuffer.Unlock();
    }

    public void Dispose() => _vertexBuffer.Dispose();
}
