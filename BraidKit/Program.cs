using System.Globalization;

namespace BraidKit;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var parseResult = Commands.Commands.RootCommand.Parse(args);
        var exitCode = await parseResult.InvokeAsync();
        return exitCode;
    }
}
