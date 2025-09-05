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
            verts.Add(new(cos * innerRadius, sin * innerRadius, 0f, cos, sin, 0f)); // Inner vertex can move along normal to make the line wider
            verts.Add(new(cos * outerRadius, sin * outerRadius, 0f, 0f, 0f, 0f)); // Outer vertex doesn't move
        }
        return verts;
    }

    private const float _sqrt2 = 1.4142135623730951f; // Normal is not normalized since we want line width to be intuitive
    public static List<LineVertex> GetRectangleOutlineTriangleStrip(float xMin, float xMax, float yMin, float yMax, float thickness = 0f) =>
    [
        new(xMin, yMax, 0f, 0f, 0f, 0f),
        new(xMin + thickness, yMax - thickness, 0f, -_sqrt2,  _sqrt2, 0f),
        new(xMax, yMax, 0f, 0f, 0f, 0f),
        new(xMax - thickness, yMax - thickness, 0f,  _sqrt2,  _sqrt2, 0f),
        new(xMax, yMin, 0f, 0f, 0f, 0f),
        new(xMax - thickness, yMin + thickness, 0f,  _sqrt2, -_sqrt2, 0f),
        new(xMin, yMin, 0f, 0f, 0f, 0f),
        new(xMin + thickness, yMin + thickness, 0f, -_sqrt2, -_sqrt2, 0f),
        new(xMin, yMax, 0f, 0f, 0f, 0f),
        new(xMin + thickness, yMax - thickness, 0f, -_sqrt2,  _sqrt2, 0f),
    ];
}
