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
            using var highPrecisionTimer = watchSettings.HighPrecision ? new HighPrecisionTimer(10) : null;

            while (!cancel && braidGame.IsRunning)
            {
                var value = watchSettings.ValueSelector(braidGame);
                var color = watchSettings.GetColor(value);
                using var __ = color != null ? new TempConsoleColor(color.Value) : null;
                const int maxWidth = 5;
                Console.Write($"\r{value,-maxWidth:0}\r");

                Thread.Sleep(5); // Reduce CPU usage
            }

            ConsoleHelper.WriteWarning($"\rStopped watching {watchValue}");
        });

    private static WatchValueSettings GetSettings(WatchValue watchValue) => watchValue switch
    {
        WatchValue.TimPosX => new(x => x.GetTimOrNull()?.PositionX ?? 0f),
        WatchValue.TimPosY => new(x => x.GetTimOrNull()?.PositionY ?? 0f),
        WatchValue.TimSpeedX => new(x => MathF.Abs(x.GetTimOrNull()?.VelocityX ?? 0f), (200f, ConsoleColor.Blue), (230f, ConsoleColor.Green), (240f, ConsoleColor.Red)),
        WatchValue.TimSpeedY => new(x => MathF.Abs(x.GetTimOrNull()?.VelocityY ?? 0f), (833f, ConsoleColor.Blue)),
        WatchValue.FrameIndex => new(x => x.FrameCount),
        WatchValue.RewindFrames => GetRewindFramesSettings(),
        _ => throw new ArgumentOutOfRangeException(nameof(watchValue), watchValue, null),
    };

    private static WatchValueSettings GetRewindFramesSettings()
    {
        int? rewindStartFrame = null;
        int rewindFrames = 0;

        return new(x =>
        {
            var rewinding = x.Rewinding.Value;

            if (!rewinding)
            {
                rewindStartFrame = null;
                return rewindFrames;
            }

            var frameIndex = x.FrameCount.Value;
            rewindStartFrame ??= frameIndex;
            rewindFrames = frameIndex - rewindStartFrame.Value + 1;
            return rewindFrames;
        }, (0f, ConsoleColor.White), (1f, ConsoleColor.Green), (2f, ConsoleColor.Blue), (3f, ConsoleColor.Red))
        {
            HighPrecision = true,
        };
    }

    private record WatchValueSettings(
        ValueSelector ValueSelector,
        params (float StartValue, ConsoleColor Color)[] Colors)
    {
        public bool HighPrecision { get; init; } = false;

        public ConsoleColor? GetColor(float value) => Colors
            .Where(x => value >= x.StartValue)
            .OrderByDescending(x => x.StartValue)
            .Select(x => (ConsoleColor?)x.Color)
            .FirstOrDefault();
    }

    private delegate float ValueSelector(BraidGame game);

    private enum WatchValue
    {
        TimPosX,
        TimPosY,
        TimSpeedX,
        TimSpeedY,
        FrameIndex,
        RewindFrames,
    }
}