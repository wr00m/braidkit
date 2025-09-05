using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal class TriangleStrip<TVertex> : IDisposable
    where TVertex : unmanaged, IVertex
{
    public readonly uint PrimitiveCount;
    public readonly uint Stride = TVertex.Size;
    public readonly VertexFormat VertexFormat = TVertex.Format;
    public IDirect3DVertexBuffer9 _vertexBuffer;

    public TriangleStrip(IDirect3DDevice9 device, List<TVertex> verts)
    {
        PrimitiveCount = (uint)verts.Count - 2;

        _vertexBuffer = device.CreateVertexBuffer(
            (uint)verts.Count * Stride,
            Usage.WriteOnly,
            TVertex.Format,
            Pool.Managed);

        var buff = _vertexBuffer.Lock<TVertex>(0, 0, LockFlags.None);
        verts.CopyTo(buff);
        _vertexBuffer.Unlock();
    }

    public void Dispose() => _vertexBuffer.Dispose();
}
