namespace BraidKit.Core.Helpers;

internal static class EnumHelper
{
    /// <summary>Gets enum values in declared order, unlike <see cref="Enum.GetValues()"/></summary>
    public static List<TEnum> GetDeclaredValues<TEnum>() where TEnum : Enum =>
        typeof(TEnum)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .OrderBy(x => x.MetadataToken)
            .Select(x => (TEnum)x.GetValue(null)!)
            .ToList();
}
