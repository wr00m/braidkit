using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    internal static RootCommand RootCommand =>
        new("braidkit")
        {
            CameraLockCommand,
            CameraZoomCommand,
            TimPositionCommand,
            TimVelocityCommand,
            TimSpeedMultiplierCommand,
            TimJumpMultiplierCommand,
            TimTouchesCommand,
            EntityFlagCommand,
            ToggleFullSpeedInBackgroundCommand,
            ToggleDebugInfoCommand,
            IlTimerCommand,
            ResetPiecesCommand,
            RenderOverlayCommand,
        };
}