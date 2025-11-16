using System.CommandLine;
using System.Globalization;

namespace BraidKit.Network;

/// <summary>Use this to host server on Linux, since BraidKit.exe is Windows only</summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var serverCommand = new Command("server", "Hosts a multiplayer server")
        {
            new Option<int>("--port", "-p") { Description = "Server port", DefaultValueFactory = _ => 55555 },
        };

        serverCommand.SetAction(parseResult =>
        {
            var port = parseResult.GetRequiredValue<int>("--port");
            using var server = new Server(port);
            Console.WriteLine($"Server is running on port {port}, press any key to exit...");
            Console.ReadKey(true);
        });

        var parseResult = serverCommand.Parse(args);
        var exitCode = await parseResult.InvokeAsync();
        return exitCode;
    }
}
