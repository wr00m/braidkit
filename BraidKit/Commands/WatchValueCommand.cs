using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command WatchValueCommand =>
        new Command("watch-value", "Monitors a value in real-time")
        {
            new Argument<WatchValue>("value").FormatEnumHelp(),
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var watchValue = parseResult.GetRequiredValue<WatchValue>("value");
            var watchSettings = GetSettings(watchValue);

            Console.WriteLine($"Watching {watchValue}. Press Ctrl+C to exit.\n");
            var cancel = false;
            using var _ = new ConsoleCancelAction(() => cancel = true);

            while (!cancel && braidGame.IsRunning)
            {
                var value = watchSettings.ValueSelector(braidGame);
                var color = watchSettings.GetColor(value);
                using var __ = color != null ? new TempConsoleColor(color.Value) : null;
                const int maxWidth = 5;
                Console.Write($"\r{value,-maxWidth:0}\r");

                Thread.Sleep(10); // Reduce CPU usage
            }

            ConsoleHelper.WriteWarning($"\rStopped watching {watchValue}");
        });

    private static WatchValueSettings GetSettings(WatchValue watchValue) => watchValue switch
    {
        WatchValue.TimPosX => new(x => x.GetTimOrNull()?.PositionX ?? 0f),
        WatchValue.TimPosY => new(x => x.GetTimOrNull()?.PositionY ?? 0f),
        WatchValue.TimSpeedX => new(x => MathF.Abs(x.GetTimOrNull()?.VelocityX ?? 0f), (200f, ConsoleColor.Blue), (230f, ConsoleColor.Red)),
        WatchValue.TimSpeedY => new(x => MathF.Abs(x.GetTimOrNull()?.VelocityY ?? 0f), (833f, ConsoleColor.Blue)),
        WatchValue.FrameIndex => new(x => x.FrameCount),
        _ => throw new ArgumentOutOfRangeException(nameof(watchValue), watchValue, null),
    };

    private record WatchValueSettings(
        Func<BraidGame, float> ValueSelector,
        params (float StartValue, ConsoleColor Color)[] Colors)
    {
        public ConsoleColor? GetColor(float value) => Colors
            .Where(x => value >= x.StartValue)
            .OrderByDescending(x => x.StartValue)
            .Select(x => (ConsoleColor?)x.Color)
            .FirstOrDefault();
    }

    private enum WatchValue
    {
        TimPosX,
        TimPosY,
        TimSpeedX,
        TimSpeedY,
        FrameIndex,
    }
}