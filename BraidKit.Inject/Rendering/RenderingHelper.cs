using BraidKit.Core;
using System.Numerics;
using Vortice.Mathematics;

namespace BraidKit.Inject.Rendering;

public static class RenderingHelper
{
    public static (HAlign, VAlign) ToAlignment(this TextPosition textPosition) => textPosition switch
    {
        TextPosition.TopLeft => (HAlign.Left, VAlign.Top),
        TextPosition.MiddleLeft => (HAlign.Left, VAlign.Middle),
        TextPosition.BottomLeft => (HAlign.Left, VAlign.Bottom),
        TextPosition.TopCenter => (HAlign.Center, VAlign.Top),
        TextPosition.MiddleCenter => (HAlign.Center, VAlign.Middle),
        TextPosition.BottomCenter => (HAlign.Center, VAlign.Bottom),
        TextPosition.TopRight => (HAlign.Right, VAlign.Top),
        TextPosition.MiddleRight => (HAlign.Right, VAlign.Middle),
        TextPosition.BottomRight => (HAlign.Right, VAlign.Bottom),
        TextPosition.BelowEntity => (HAlign.Center, VAlign.Top),
        _ => throw new ArgumentOutOfRangeException(nameof(textPosition), textPosition, null),
    };

    public static Vector2 GetAlignmentAnchor(this Rect alignmentBounds, HAlign alignX, VAlign alignY) => (alignX, alignY) switch
    {
        (HAlign.Left, VAlign.Top) => alignmentBounds.TopLeft,
        (HAlign.Left, VAlign.Middle) => alignmentBounds.CenterLeft,
        (HAlign.Left, VAlign.Bottom) => alignmentBounds.BottomLeft,
        (HAlign.Center, VAlign.Top) => alignmentBounds.TopCenter,
        (HAlign.Center, VAlign.Middle) => alignmentBounds.Center,
        (HAlign.Center, VAlign.Bottom) => alignmentBounds.BottomCenter,
        (HAlign.Right, VAlign.Top) => alignmentBounds.TopRight,
        (HAlign.Right, VAlign.Middle) => alignmentBounds.CenterRight,
        (HAlign.Right, VAlign.Bottom) => alignmentBounds.BottomRight,
        _ => throw new ArgumentException($"{nameof(alignX)}, {nameof(alignY)}"),
    };
}
