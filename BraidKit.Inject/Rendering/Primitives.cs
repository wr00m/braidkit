using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal class Primitives<TVertex> : IDisposable
    where TVertex : unmanaged, IVertex
{
    private readonly IDirect3DDevice9 _device;
    private readonly uint _primitiveCount;
    private readonly uint _stride = TVertex.Size;
    private readonly VertexFormat _vertexFormat = TVertex.Format;
    private readonly PrimitiveType _primitiveType;
    private readonly List<TVertex>? _verts;
    private readonly IDirect3DVertexBuffer9? _vertexBuffer;

    public Primitives(IDirect3DDevice9 device, PrimitiveType primitiveType, List<TVertex> verts, bool useVertexBuffer = true)
    {
        _device = device;
        _primitiveType = primitiveType;
        _primitiveCount = primitiveType switch
        {
            PrimitiveType.TriangleStrip or
            PrimitiveType.TriangleFan => (uint)verts.Count - 2,
            PrimitiveType.TriangleList => (uint)verts.Count / 3,
            _ => throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null),
        };

        if (useVertexBuffer)
        {
            _vertexBuffer = device!.CreateVertexBuffer(
                (uint)verts.Count * _stride,
                Usage.WriteOnly,
                TVertex.Format,
                Pool.Managed);

            var buff = _vertexBuffer.Lock<TVertex>(0, 0, LockFlags.None);
            verts.CopyTo(buff);
            _vertexBuffer.Unlock();
        }
        else
            _verts = verts;
    }

    public void Dispose() => _vertexBuffer?.Dispose();

    public unsafe void Render()
    {
        _device.VertexFormat = _vertexFormat;

        if (_vertexBuffer != null)
        {
            _device.SetStreamSource(0, _vertexBuffer, 0, _stride);
            _device.DrawPrimitive(_primitiveType, 0, _primitiveCount);
        }
        else
            fixed (TVertex* ptr = CollectionsMarshal.AsSpan(_verts))
                _device.DrawPrimitiveUP(_primitiveType, _primitiveCount, (nint)ptr, _stride);
    }
}
