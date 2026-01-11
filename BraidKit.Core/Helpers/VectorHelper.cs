using System.Numerics;

namespace BraidKit.Core.Helpers;

public static class VectorHelper
{
    public static Vector2 GetNormalized(this Vector2 v) => Vector2.Normalize(v);
    public static Vector2 GetPerpendicularCW(this Vector2 v) => new(v.Y, -v.X);

    /// <returns>The vector's direction in radians [-π,π] relative to the +X axis</returns>
    public static float GetRadians(this Vector2 v) => MathF.Atan2(v.Y, v.X);
}
