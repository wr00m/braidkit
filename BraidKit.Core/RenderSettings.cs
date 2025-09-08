using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Explicit)]
public struct RenderSettings()
{
    public const bool DefaultRenderColliders = true;
    public const bool DefaultRenderVelocity = true;
    public const float DefaultLineWidth = 2f;
    public const float DefaultFontSize = 10f;
    public const uint DefaultFontColor = 0xffffff00; // RGBA (AABBGGRR because of little-endian)

    [FieldOffset(0)] public bool RenderColliders = DefaultRenderColliders;
    [FieldOffset(4)] public bool RenderVelocity = DefaultRenderVelocity;
    [FieldOffset(8)] public float LineWidth = DefaultLineWidth;
    [FieldOffset(12)] public float FontSize = DefaultFontSize;
    [FieldOffset(16)] public uint FontColor = DefaultFontColor;
}
