using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command TimPositionCommand =>
        new Command("tim-position", "Sets Tim's position")
        {
            new Argument<float?>("x") { Description = "Tim's x position" },
            new Argument<float?>("y") { Description = "Tim's y position" },
            new Option<bool>("--relative", "-r") { Description = "Relative to current position" },
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var relative = parseResult.GetValue<bool>("--relative");
            var tim = braidGame.GetTim();
            if (parseResult.GetValue<float?>("x") is float x)
            {
                tim.PositionX.Value = relative ? tim.PositionX + x : x;
                tim.DetachFromGround();
            }
            if (parseResult.GetValue<float?>("y") is float y)
            {
                tim.PositionY.Value = relative ? tim.PositionY + y : y;
                tim.DetachFromGround();
            }

            OutputTimPosition(braidGame);
        }, watermark: true);

    private static void OutputTimPosition(BraidGame braidGame)
        => Console.WriteLine($"Tim's position is x={braidGame.GetTim().PositionX:0.##} y={braidGame.GetTim().PositionY:0.##}");
}