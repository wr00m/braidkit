using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using System.CommandLine;
using System.Runtime.InteropServices;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command IlTimerCommand =>
        new Command("il-timer", "Prints level complete times")
        {
            // TODO: Aliases should be single-letter
            new Option<int?>("--world", "-w") { Description = "Only use timer for this world" },
            new Option<int?>("--level", "-l") { Description = "Only use timer for this level" },
            new Option<bool>("--live", "-t") { Description = "Use live timer" },
            new Option<bool>("--reset-pieces", "-rp") { Description = "Reset ALL pieces on door entry" },
            new Option<bool>("--high-precision", "-hp") { Description = "Increases system timer resolution" },
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var world = parseResult.GetValue<int?>("--world");
            var level = parseResult.GetValue<int?>("--level");
            var live = parseResult.GetValue<bool>("--live");
            var resetPieces = parseResult.GetValue<bool>("--reset-pieces");
            var highPrecision = parseResult.GetValue<bool>("--high-precision");
            var cancel = false;

            Console.WriteLine("IL timer enabled. Press Ctrl+C to exit.\n");
            using var _ = new ConsoleCancelAction(() => cancel = true);

            using var highPrecisionTimer = highPrecision ? new HighPrecisionTimer(10) : null;
            var ilTimer = new IlTimer(braidGame, world, level, resetPieces, live);

            while (!cancel && braidGame.IsRunning)
                SpinWait.SpinUntil(() => ilTimer.Tick(), 5);

            ConsoleHelper.WriteWarning("\rIL timer stopped");
        });
}

internal class IlTimer
{
    private readonly BraidGame _braidGame;
    private readonly int? _onlyWorld;
    private readonly int? _onlyLevel;
    private readonly bool _resetPieces;
    private readonly bool _liveTimer;
    private int _currentWorld;
    private int _currentLevel;
    private bool _stopped;
    private bool _paused;
    private int _frameIndex;
    private int _levelFrameCount;
    private bool _hasMissedImportantFrames; // True if we missed frames at start/pause/unpause/stop
    private const double _fps = 60.0;
    private double LevelSeconds => _levelFrameCount / _fps;

    public IlTimer(BraidGame braidGame, int? onlyWorld = null, int? onlyLevel = null, bool resetPieces = false, bool liveTimer = false)
    {
        _braidGame = braidGame;
        _onlyWorld = onlyWorld;
        _onlyLevel = onlyLevel;
        _resetPieces = resetPieces;
        _liveTimer = liveTimer;
        Restart();
    }

    private void Restart()
    {
        _currentWorld = _braidGame.TimWorld;
        _currentLevel = _braidGame.TimLevel;
        _stopped = (_onlyWorld != null && _currentWorld != _onlyWorld) || (_onlyLevel != null && _currentLevel != _onlyLevel);
        _paused = false;
        _frameIndex = _braidGame.FrameCount;
        _levelFrameCount = 0;
        _hasMissedImportantFrames = false;
    }

    private void Stop() => _stopped = true;

    /// <returns>True if a new frame was handled</returns>
    public bool Tick()
    {
        // Early exit if we have already polled this frame
        var prevFrameIndex = _frameIndex;
        _frameIndex = _braidGame.FrameCount;
        if (_frameIndex == prevFrameIndex)
            return false; // Keep polling

        var frameDelta = _frameIndex - prevFrameIndex;
        var hasMissedFrames = frameDelta > 1;

        // Restart timer if level has changed
        if (_braidGame.TimWorld != _currentWorld || _braidGame.TimLevel != _currentLevel)
        {
            Restart();
            //_levelFrameCount += frameDelta; // TODO: Timer is usually 1 frame too fast, but this doesn't seem to fix it...
            _hasMissedImportantFrames |= hasMissedFrames;
            return true;

            // TODO: Stop timer if level has changed; restart when _braidGame.TimEnterLevel
        }

        // Early exit if timer is stopped
        if (_stopped)
            return true;

        // Pause timer if puzzle assembly or main menu screen is active
        var paused = _braidGame.InPuzzleAssemblyScreen || _braidGame.InMainMenu;
        if (paused != _paused)
        {
            _hasMissedImportantFrames |= hasMissedFrames;
            _paused = paused;
        }

        if (!_paused)
            _levelFrameCount += frameDelta;

        if (_liveTimer)
        {
            using var _ = new TempConsoleColor(_paused ? ConsoleColor.DarkYellow : ConsoleColor.Blue);
            Console.Write($"\r{LevelSeconds:0.00}");
        }

        // Stop timer if level is finished
        if (_braidGame.TimIsEnteringDoor || _braidGame.TimHasTouchedFlagpole)
        {
            _hasMissedImportantFrames |= hasMissedFrames;
            Stop();

            Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r"); // Clear live timer
            Console.WriteLine($"Level: {_currentWorld}-{_currentLevel}");
            Console.WriteLine($"Time: {LevelSeconds:0.00}");
            if (_hasMissedImportantFrames)
                ConsoleHelper.WriteWarning("Retiming needed due to dropped frames");
            Console.WriteLine();

            // TODO: Maybe this should be moved to Restart() so reset also happens when F1 is pressed?
            if (_resetPieces)
                _braidGame.ResetPieces();
        }

        return true;
    }
}

/// <summary>
/// Increases system timer resolution, allowing Thread.Sleep() and timers to be more accurate. Use with care.
/// </summary>
internal class HighPrecisionTimer : IDisposable
{
    private readonly uint _resolutionMs;
    private bool _disposed = false;

    public HighPrecisionTimer(uint resolutionMs = 1)
    {
        _resolutionMs = resolutionMs;
        _ = timeBeginPeriod(_resolutionMs);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ = timeEndPeriod(_resolutionMs);
        GC.SuppressFinalize(this);
    }

    // Finalizer, in case someone forgets to call Dispose()
    ~HighPrecisionTimer() => Dispose();

    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint uMilliseconds);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint uMilliseconds);
}