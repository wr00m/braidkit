namespace BraidKit.Inject;

internal static class Logger
{
    public static void Log(params string[] lines) => File.AppendAllLines("braidkit.log", lines);
    public static void Log(Exception ex) => Log(ex.ToString());
}
