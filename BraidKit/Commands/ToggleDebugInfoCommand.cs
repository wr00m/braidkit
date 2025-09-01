using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command ToggleDebugInfoCommand =>
        new Command("debug-info", "Toggles in-game debug info")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.DrawDebugInfo.Value = !braidGame.DrawDebugInfo.Value;
            OutputShowDebugInfo(braidGame);
        });

    private static void OutputShowDebugInfo(BraidGame braidGame)
        => Console.WriteLine($"Debug info is {(braidGame.DrawDebugInfo ? "on" : "off")}");
}