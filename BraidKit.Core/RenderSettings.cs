using BraidKit.Core.Helpers;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Explicit)]
public struct RenderSettings()
{
    public const bool DefaultRenderEntityBounds = true;
    public const bool DefaultRenderEntityCenters = true;
    public const TextPosition DefaultRenderTimVelocity = TextPosition.BelowEntity;
    public const bool DefaultRenderAllEntities = false;
    public const bool DefaultRenderBorder = false;
    public const float DefaultLineWidth = 1f;
    public const float DefaultFontSize = 15f;
    public static readonly Color DefaultFontColor = ColorHelper.Cyan;
    public static readonly Color DefaultLineColor = ColorHelper.Empty;

    [FieldOffset(0)] public bool RenderEntityBounds = DefaultRenderEntityBounds;
    [FieldOffset(4)] public bool RenderEntityCenters = DefaultRenderEntityCenters;
    [FieldOffset(8)] public TextPosition RenderTimVelocity = DefaultRenderTimVelocity;
    [FieldOffset(12)] public bool RenderAllEntities = DefaultRenderAllEntities;
    [FieldOffset(16)] public bool RenderBorder = DefaultRenderBorder;
    [FieldOffset(20)] public float LineWidth = DefaultLineWidth;
    [FieldOffset(24)] public float FontSize = DefaultFontSize;
    [FieldOffset(28)] public Color FontColor = DefaultFontColor;
    [FieldOffset(32)] public Color LineColor = DefaultLineColor;

    public readonly bool IsRenderingActive() => RenderEntityBounds || RenderEntityCenters || RenderTimVelocity != TextPosition.None || RenderBorder;
    public readonly bool IsLineColorActive() => LineColor != DefaultLineColor;

    public static RenderSettings Off => new()
    {
        RenderEntityBounds = false,
        RenderEntityCenters = false,
        RenderTimVelocity = TextPosition.None,
    };
}

public enum TextPosition
{
    None,
    BelowEntity,
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}