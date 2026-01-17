using BraidKit.Core.Helpers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Mathematics;

namespace BraidKit.Inject.Rendering;

internal static class Geometry
{
    public static List<LineVertex> GetCircleOutlineTriangleStrip(float innerRadius, float outerRadius, int segments = 32)
    {
        var verts = new List<LineVertex>((segments + 1) * 2);
        for (int i = 0; i <= segments; i++)
        {
            var angle = (i % segments) / (float)segments * MathF.Tau;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            verts.Add(new(cos * innerRadius, sin * innerRadius, cos, sin, 1f)); // Inner vertex can move along normal to make the line wider
            verts.Add(new(cos * outerRadius, sin * outerRadius, 0f, 0f, 0f)); // Outer vertex doesn't move
        }
        return verts;
    }

    private const float _sqrt2 = 1.4142135623730951f; // Normal is not normalized since we want line width to be intuitive
    public static List<LineVertex> GetRectangleOutlineTriangleStrip(float xMin, float xMax, float yMin, float yMax, float thickness = 0f) =>
    [
        new(xMin, yMax, 0f, 0f, 0f),
        new(xMin + thickness, yMax - thickness, -_sqrt2,  _sqrt2, 1f),
        new(xMax, yMax, 0f, 0f, 0f),
        new(xMax - thickness, yMax - thickness,  _sqrt2,  _sqrt2, 1f),
        new(xMax, yMin, 0f, 0f, 0f),
        new(xMax - thickness, yMin + thickness,  _sqrt2, -_sqrt2, 1f),
        new(xMin, yMin, 0f, 0f, 0f),
        new(xMin + thickness, yMin + thickness, -_sqrt2, -_sqrt2, 1f),
        new(xMin, yMax, 0f, 0f, 0f),
        new(xMin + thickness, yMax - thickness, -_sqrt2,  _sqrt2, 1f),
    ];

    public static List<LineVertex> GetPlusSignTriangleList(float radius)
    {
        const float wHalf = _sqrt2 / 2f;

        var quads = new List<LineVertex>
        {
            // Vertical line
            new(0f, radius, -wHalf, 0f, 0f),
            new(0f, radius, wHalf, 0f, 1f),
            new(0f, -radius, wHalf, 0f, 1f),
            new(0f, -radius, -wHalf, 0f, 0f),

            // Horizontal line
            new(-radius, 0f, 0f, -wHalf, 0f),
            new(-radius, 0f, 0f, wHalf, 1f),
            new(radius, 0f, 0f, wHalf, 1f),
            new(radius, 0f, 0f, -wHalf, 0f),
        };

        return quads.QuadsToTriangles();
    }

    public static List<TexturedVertex> GetRectangleTriangleList(Rect rect) => QuadsToTriangles<TexturedVertex>([
        new(rect.Left, rect.Bottom, 0f, 0f),
        new(rect.Left, rect.Top, 0f, 1f),
        new(rect.Right, rect.Top, 1f, 1f),
        new(rect.Right, rect.Bottom, 1f, 0f),
    ]);

    public static List<Vector2> GetRoundedRectangleTriangleList(Rect rect, float cornerRadius, int cornerSegments)
    {
        var result = new List<Vector2>();

        // Shrink corner radius if necessary to fit rectangle
        cornerRadius = MathF.Min(cornerRadius, MathF.Min(rect.Width * .5f, rect.Height * .5f));

        // Shrink rectangle to accommodate the radius
        rect.Inflate(-cornerRadius, -cornerRadius);
        Vector2[] corners = [rect.BottomLeft, rect.TopLeft, rect.TopRight, rect.BottomRight];
        Vector2[] tangents = [new(-1, 0), new(0, -1), new(1, 0), new(0, 1)];

        // Inner rectangle
        result.AddRange(QuadsToTriangles(corners));

        // Corners and sides
        for (int i = 0; i < 4; i++)
        {
            var corner = corners[i];
            var nextCorner = corners[i < 3 ? i + 1 : 0];
            var tangent = tangents[i];
            var offset = tangent * cornerRadius;

            // Side outer rectangle
            result.AddRange(QuadsToTriangles([corner, corner + offset, nextCorner + offset, nextCorner]));

            // Corner arc
            var toRads = tangent.GetRadians();
            var fromRads = toRads - MathF.PI * .5f;
            result.AddRange(GetArcTriangleFan(corner, cornerRadius, fromRads, toRads, cornerSegments).TriangleFanToTriangles());
        }

        return result;
    }

    public static List<Vector2> GetArcTriangleFan(Vector2 center, float radius, float fromRads, float toRads, int segments)
    {
        // Build triangle fan from center
        List<Vector2> triFan = [center];

        for (int i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var rads = Lerp(fromRads, toRads, t);
            var cos = MathF.Cos(rads);
            var sin = MathF.Sin(rads);
            triFan.Add(center + new Vector2(cos * radius, sin * radius));
        }

        return triFan;
    }

    public static List<TexturedVertex> GetSpeechBubbleTriangleList(Rect rect, Vector2 tailSize, float cornerRadius, int cornerSegments = 4)
    {
        // Bubble part (containing the text)
        var tris = GetRoundedRectangleTriangleList(rect, cornerRadius, cornerSegments);

        // Tail part (pointing to the character speaking)
        if (tailSize != Vector2.Zero)
        {
            tailSize = new(MathF.Min(rect.Width, tailSize.X), tailSize.Y); // Tail can't be wider than bubble
            tris.AddRange([
                new(rect.Center.X - tailSize.X * .5f, rect.Bottom),
                new(rect.Center.X + tailSize.X * .5f, rect.Bottom),
                new(rect.Center.X, rect.Bottom + tailSize.Y),
            ]);
        }

        return [.. tris.Select(x => new TexturedVertex(x.X, x.Y, 0f, 0f))];
    }

    private static List<TVertex> QuadsToTriangles<TVertex>(this IList<TVertex> verts)
    {
        if (verts.Count % 4 != 0)
            throw new ArgumentException("Invalid quad vertex list");

        var result = new List<TVertex>(verts.Count / 4 * 6);
        for (int i = 0; i < verts.Count; i += 4)
        {
            result.AddRange([verts[i], verts[i + 1], verts[i + 2]]);
            result.AddRange([verts[i], verts[i + 2], verts[i + 3]]);
        }
        return result;
    }

    private static List<TVertex> TriangleFanToTriangles<TVertex>(this List<TVertex> verts)
    {
        if (verts.Count < 3)
            throw new ArgumentException("Invalid triangle fan vertex list");

        var result = new List<TVertex>(verts.Count - 2);
        for (int i = 2; i < verts.Count; i++)
            result.AddRange([verts[0], verts[i - 1], verts[i]]);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
