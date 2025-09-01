using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command TimVelocityCommand =>
        new Command("tim-velocity", "Sets Tim's velocity")
        {
            new Argument<float?>("x") { Description = "Tim's x velocity" },
            new Argument<float?>("y") { Description = "Tim's y velocity" },
            new Option<bool>("--relative", "-r") { Description = "Relative to current velocity" },
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var relative = parseResult.GetValue<bool>("--relative");
            var tim = braidGame.GetTim();
            if (parseResult.GetValue<float?>("x") is float x)
            {
                tim.VelocityX.Value = relative ? tim.VelocityX + x : x;
                tim.DetachFromGround();
            }
            if (parseResult.GetValue<float?>("y") is float y)
            {
                tim.VelocityY.Value = relative ? tim.VelocityY + y : y;
                tim.DetachFromGround();
            }
            OutputTimVelocity(braidGame);
        }, watermark: true);

    private static void OutputTimVelocity(BraidGame braidGame)
        => Console.WriteLine($"Tim's velocity is x={braidGame.GetTim().VelocityX:0.##} y={braidGame.GetTim().VelocityY:0.##}");
}