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

    public static List<TexturedVertex> GetRectangleTriangleList(float xMin, float xMax, float yMin, float yMax) => QuadsToTriangles<TexturedVertex>([
        new(xMin, yMin, 0f, 0f),
        new(xMin, yMax, 0f, 1f),
        new(xMax, yMax, 1f, 1f),
        new(xMax, yMin, 1f, 0f),
    ]);

    private static List<TVertex> QuadsToTriangles<TVertex>(this List<TVertex> verts) where TVertex : IVertex
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
}
