using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using System.CommandLine;

namespace BraidKit.Commands;

internal static class CommandHelper
{
    internal static Command SetBraidGameAction(this Command cmd, Action<BraidGame, ParseResult> action, bool watermark = false)
    {
        cmd.SetAction(parseResult =>
        {
            using var braidGame = BraidGame.GetFromOtherProcess();

            if (braidGame is null)
            {
                ConsoleHelper.WriteError("Braid is not running");
                return 1;
            }

            if (!braidGame.IsSteamVersion)
            {
                ConsoleHelper.WriteError("Only Steam version of Braid is supported");
                return 1;
            }

            if (watermark)
                braidGame.AddWatermark();

            action(braidGame, parseResult);
            return 0;
        });
        return cmd;
    }

    internal static Argument<TEnum> FormatEnumArgumentHelp<TEnum>(this Argument<TEnum> arg) where TEnum : struct, Enum
    {
        arg.HelpName = arg.Name;
        arg.Description = string.Join("|", Enum.GetValues<TEnum>().Select(x => x.ToString().ToLowerInvariant()).OrderBy(x => x));
        return arg;
    }
}
