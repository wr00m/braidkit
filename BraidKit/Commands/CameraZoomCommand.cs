using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command CameraZoomCommand =>
        new Command("camera-zoom", "Sets camera zoom (experimental, may cause issues with the GUI)")
        {
            new Argument<float?>("zoom") { Description="Default is 1, <1 zooms out, >1 zooms in" },
            CameraZoomResetCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            if (parseResult.GetValue<float?>("zoom") is float zoom)
                braidGame.Zoom = zoom;
            OutputCameraZoom(braidGame);
        });

    private static Command CameraZoomResetCommand =>
        new Command("reset", "Resets camera zoom to default value")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            const float defaultZoom = 1;
            braidGame.Zoom = defaultZoom;
            OutputCameraZoom(braidGame);
        });

    private static void OutputCameraZoom(BraidGame braidGame)
        => Console.WriteLine($"Camera zoom is {braidGame.Zoom:0.##}");
}