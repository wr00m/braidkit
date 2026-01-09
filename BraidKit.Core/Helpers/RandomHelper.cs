namespace BraidKit.Core.Helpers;

public static class RandomHelper
{
    public static float GetRandomFloat(this Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

}
