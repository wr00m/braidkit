using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command ToggleFullSpeedInBackgroundCommand =>
        new Command("bg-full-speed", "Toggles game running at full speed in background")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.FullSpeedInBackground = !braidGame.FullSpeedInBackground;
            OutputFullSpeedInBackground(braidGame);
        }, watermark: true);

    private static void OutputFullSpeedInBackground(BraidGame braidGame)
        => Console.WriteLine($"Full game speed in background is {(braidGame.FullSpeedInBackground ? "on" : "off")}");
}