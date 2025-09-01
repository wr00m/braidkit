using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command TimSpeedMultiplierCommand =>
        new Command("tim-speed", "Sets Tim's movement speed multiplier")
        {
            new Argument<float?>("multiplier") { Description = "Tim's speed multiplier" },
            TimSpeedMultiplierResetCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            if (parseResult.GetValue<float?>("multiplier") is float multiplier)
                braidGame.TimSpeedMultiplier = multiplier;
            OutputTimSpeedMultiplier(braidGame);
        }, watermark: true);

    private static Command TimSpeedMultiplierResetCommand =>
        new Command("reset", "Resets Tim's movement speed multiplier")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.TimSpeedMultiplier = 1;
            OutputTimSpeedMultiplier(braidGame);
        });

    private static void OutputTimSpeedMultiplier(BraidGame braidGame)
        => Console.WriteLine($"Tim's speed multiplier is {braidGame.TimSpeedMultiplier:0.##}");
}