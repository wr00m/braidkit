using BraidKit.Core.Helpers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace BraidKit.Inject.Rendering;

internal static class Direct3DExtensions
{
    public static unsafe IntPtr GetEndSceneAddr(this IDirect3DDevice9 device)
    {
        var vtable = *(IntPtr**)device.NativePointer;
        var endScenePtr = vtable[42];
        return endScenePtr;
    }

    public static unsafe void RenderTriangles<TVertex>(this IDirect3DDevice9 device, List<TVertex> verts) where TVertex : unmanaged, IVertex
    {
        device.VertexFormat = TVertex.Format;
        fixed (TVertex* ptr = CollectionsMarshal.AsSpan(verts))
            device.DrawPrimitiveUP(PrimitiveType.TriangleList, (uint)verts.Count / 3, (nint)ptr, TVertex.Size);
    }

    public static void RenderTriangleStrip<TVertex>(this IDirect3DDevice9 device, TriangleStrip<TVertex> triStrip) where TVertex : unmanaged, IVertex
    {
        device.SetStreamSource(0, triStrip._vertexBuffer, 0, triStrip.Stride);
        device.VertexFormat = triStrip.VertexFormat;
        device.DrawPrimitive(PrimitiveType.TriangleStrip, 0, triStrip.PrimitiveCount);
    }

    public static IDirect3DTexture9 LoadTexture(this IDirect3DDevice9 device, string textureFilename)
    {
        using var bmp = Assembly.GetExecutingAssembly().ReadEmbeddedImageFile(textureFilename);

        var bmpData = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            bmp.PixelFormat);

        var texture = device.CreateTexture(
            (uint)bmpData.Width,
            (uint)bmpData.Height,
            1,
            Usage.None,
            bmp.PixelFormat.ToDirect3DFormat(),
            Pool.Managed);

        var textureData = texture.LockRect(0, LockFlags.None);

        unsafe
        {
            byte* srcPtr = (byte*)bmpData.Scan0.ToPointer();
            byte* destPtr = (byte*)textureData.DataPointer.ToPointer();

            for (int y = 0; y < bmpData.Height; y++)
                Buffer.MemoryCopy(srcPtr + y * bmpData.Stride, destPtr + y * textureData.Pitch, textureData.Pitch, bmpData.Width * bmpData.PixelFormat.GetBytesPerPixel());
        }

        texture.UnlockRect(0);
        bmp.UnlockBits(bmpData);

        return texture;
    }

    public static Format ToDirect3DFormat(this PixelFormat pixelFormat) => pixelFormat switch
    {
        PixelFormat.Format32bppArgb => Format.A8R8G8B8,
        _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null),
    };

    public static int GetBytesPerPixel(this PixelFormat pixelFormat) => pixelFormat switch
    {
        PixelFormat.Format32bppArgb => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null),
    };
}
