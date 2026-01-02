using BraidKit.Core.Network;
using System.CommandLine;
using System.Globalization;

namespace BraidKit.ServerApp;

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
            new Option<int>("--web-port", "-w") { Description = "Web dashboard port", DefaultValueFactory = _ => 8080 },
        };

        serverCommand.SetAction(async parseResult =>
        {
            var port = parseResult.GetRequiredValue<int>("--port");
            var webPort = parseResult.GetRequiredValue<int>("--web-port");

            using var server = new Server(port);

            Console.WriteLine($"Game server is running on port {port}");
            Console.WriteLine($"Web server is running on port {webPort}");

            await Task.WhenAll([
                server.MainLoop(CancellationToken.None),
                WebDashboard.RunWebDashboard(server, webPort),
            ]);
        });

        var parseResult = serverCommand.Parse(args);
        var exitCode = await parseResult.InvokeAsync();
        return exitCode;
    }
}
