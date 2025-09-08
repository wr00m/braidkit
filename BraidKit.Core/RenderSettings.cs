using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Explicit)]
public struct RenderSettings()
{
    public const bool DefaultRenderColliders = true;
    public const bool DefaultRenderVelocity = true;
    public const float DefaultLineWidth = 2f;
    public const float DefaultFontSize = 10f;

    [FieldOffset(0)] public bool RenderColliders = DefaultRenderColliders;
    [FieldOffset(4)] public bool RenderVelocity = DefaultRenderVelocity;
    [FieldOffset(8)] public float LineWidth = DefaultLineWidth;
    [FieldOffset(12)] public float FontSize = DefaultFontSize;
}
