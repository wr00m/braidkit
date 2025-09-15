using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Explicit)]
public struct RenderSettings()
{
    public const bool DefaultRenderEntityBounds = true;
    public const bool DefaultRenderEntityCenters = true;
    public const bool DefaultRenderTimVelocity = true;
    public const bool DefaultRenderAllEntities = false;
    public const float DefaultLineWidth = 1f;
    public const float DefaultFontSize = 10f;
    public const uint DefaultFontColor = 0xffffff00; // RGBA (AABBGGRR because of little-endian)
    public const uint DefaultLineColor = 0x00000000; // RGBA (AABBGGRR because of little-endian)

    [FieldOffset(0)] public bool RenderEntityBounds = DefaultRenderEntityBounds;
    [FieldOffset(4)] public bool RenderEntityCenters = DefaultRenderEntityCenters;
    [FieldOffset(8)] public bool RenderTimVelocity = DefaultRenderTimVelocity;
    [FieldOffset(12)] public bool RenderAllEntities = DefaultRenderAllEntities;
    [FieldOffset(16)] public float LineWidth = DefaultLineWidth;
    [FieldOffset(20)] public float FontSize = DefaultFontSize;
    [FieldOffset(24)] public uint FontColor = DefaultFontColor;
    [FieldOffset(28)] public uint LineColor = DefaultLineColor;

    public readonly bool IsRenderingActive() => RenderEntityBounds || RenderEntityCenters || RenderTimVelocity;
    public readonly bool IsLineColorActive() => LineColor != DefaultLineColor;
}
