using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command TimJumpMultiplierCommand =>
        new Command("tim-jump", "Sets Tim's jump speed multiplier")
        {
            new Argument<float?>("multiplier") { Description = "Tim's jump multiplier" },
            TimJumpMultiplierResetCommand
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            if (parseResult.GetValue<float?>("multiplier") is float multiplier)
                braidGame.TimJumpMultiplier = multiplier;
            OutputTimJumpMultiplier(braidGame);
        }, watermark: true);

    private static Command TimJumpMultiplierResetCommand =>
        new Command("reset", "Resets Tim's jump speed multiplier")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.TimJumpMultiplier = 1;
            OutputTimJumpMultiplier(braidGame);
        });

    private static void OutputTimJumpMultiplier(BraidGame braidGame)
        => Console.WriteLine($"Tim's jump multiplier is {braidGame.TimJumpMultiplier:0.##}");
}