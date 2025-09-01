using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command CameraLockCommand =>
        new Command("camera-lock", "Locks camera position")
        {
            new Argument<float?>("x") { Description = "Camera x position" },
            new Argument<float?>("y") { Description = "Camera y position" },
            new Option<bool>("--relative", "-r") { Description = "Relative to current position" },
            CameraLockUnlockCommand,
            CameraLockToggleCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var relative = parseResult.GetValue<bool>("--relative");
            braidGame.CameraLock = true;
            if (parseResult.GetValue<float?>("x") is float x)
                braidGame.CameraPositionX.Value = relative ? braidGame.CameraPositionX + x : x;
            if (parseResult.GetValue<float?>("y") is float y)
                braidGame.CameraPositionY.Value = relative ? braidGame.CameraPositionY + y : y;
            OutputCameraPosition(braidGame);
        });

    private static Command CameraLockToggleCommand =>
        new Command("toggle", "Toggles camera position locked/unlocked")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.CameraLock = !braidGame.CameraLock;
            OutputCameraPosition(braidGame);
        });

    private static Command CameraLockUnlockCommand =>
        new Command("unlock", "Unlocks camera position")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.CameraLock = false;
            OutputCameraPosition(braidGame);
        });

    private static void OutputCameraPosition(BraidGame braidGame)
        => Console.WriteLine($"Camera is {(braidGame.CameraLock ? "locked" : "unlocked")} at x={braidGame.CameraPositionX:0.##} y={braidGame.CameraPositionY:0.##}");
}