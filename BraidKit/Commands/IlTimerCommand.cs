using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command IlTimerCommand =>
        new Command("il-timer", "Prints level complete times")
        {
            new Option<int?>("--world", "-w") { Description = "Only use timer for this world" },
            new Option<int?>("--level", "-l") { Description = "Only use timer for this level" },
            new Option<bool>("--live", "-t") { Description = "Use live timer" },
            // TODO: Aliases should be single-letter
            new Option<bool>("--reset-pieces", "-rp") { Description = "Reset ALL pieces on door entry" },
            new Option<bool>("--restore-pieces", "-ri") { Description = "Restore puzzle pieces to initial state on level entry (useful for levels where puzzleboard is visible)" },
            new Option<bool>("--high-precision", "-hp") { Description = "Increases system timer resolution" },
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var world = parseResult.GetValue<int?>("--world");
            var level = parseResult.GetValue<int?>("--level");
            var live = parseResult.GetValue<bool>("--live");
            var resetPieces = parseResult.GetValue<bool>("--reset-pieces");
            var restorePieces = parseResult.GetValue<bool>("--restore-pieces");
            var highPrecision = parseResult.GetValue<bool>("--high-precision");
            var cancel = false;

            Console.WriteLine("IL timer enabled. Press Ctrl+C to exit.\n");
            using var _ = new ConsoleCancelAction(() => cancel = true);

            using var highPrecisionTimer = highPrecision ? new HighPrecisionTimer(10) : null;

            var resetPiecesMode = restorePieces ? ResetPiecesMode.RestoreInitial : resetPieces ? ResetPiecesMode.Reset : ResetPiecesMode.Off;
            var ilTimer = new IlTimer(braidGame, world, level, resetPiecesMode, live);

            while (!cancel && braidGame.IsRunning)
                SpinWait.SpinUntil(() => ilTimer.Tick(), 5);

            ConsoleHelper.WriteWarning("\rIL timer stopped");
        });
}

internal enum ResetPiecesMode
{
    Off,
    Reset,
    RestoreInitial,
}

internal class IlTimer
{
    private readonly BraidGame _braidGame;
    private readonly int? _onlyWorld;
    private readonly int? _onlyLevel;
    private readonly ResetPiecesMode _resetPiecesMode;
    private readonly bool _liveTimer;
    private readonly PuzzlePieceData[,] _puzzlePiecesSnapshot;
    private int _currentWorld;
    private int _currentLevel;
    private bool _stopped;
    private bool _paused;
    private int _frameIndex;
    private int _levelFrameCount;
    private bool _hasMissedImportantFrames; // True if we missed frames at start/pause/unpause/stop
    private const double _fps = 60.0;
    private double LevelSeconds => _levelFrameCount / _fps;

    public IlTimer(BraidGame braidGame, int? onlyWorld = null, int? onlyLevel = null, ResetPiecesMode resetPiecesMode = ResetPiecesMode.Off, bool liveTimer = false)
    {
        _braidGame = braidGame;
        _onlyWorld = onlyWorld;
        _onlyLevel = onlyLevel;
        _resetPiecesMode = resetPiecesMode;
        _liveTimer = liveTimer;
        _puzzlePiecesSnapshot = resetPiecesMode is ResetPiecesMode.RestoreInitial ? _braidGame.CurrentCampaignState.GetPuzzlePiecesSnapshot() : new PuzzlePieceData[0, 0];
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

            // TODO: Reset/restore pieces should be moved (e.g., to Restart) so reset also happens when timer is stopped/paused or F1 is pressed
            if (_resetPiecesMode is ResetPiecesMode.RestoreInitial)
                _braidGame.CurrentCampaignState.RestorePuzzlePiecesFromSnapshot(_puzzlePiecesSnapshot);
            else if (_resetPiecesMode is ResetPiecesMode.Reset)
                _braidGame.CurrentCampaignState.ResetPuzzlePieces();
        }

        return true;
    }
}
