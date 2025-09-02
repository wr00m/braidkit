using BraidKit.Core;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command RenderCommand =>
        new Command("render", "Renders in-game debug overlay (experimental)")
        {
            new Option<bool>("--colliders", "-c") { Description = "Render colliders", DefaultValueFactory = _ => true },
            new Option<float>("--line-width", "-l") { Description = "Geometry outline width", DefaultValueFactory = _ => RenderSettings.DefaultLineWidth },
            RenderResetCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var isRendering = braidGame.Process.InjectRenderer(new()
            {
                RenderColliders = parseResult.GetRequiredValue<bool>("--colliders"),
                LineWidth = parseResult.GetRequiredValue<float>("--line-width"),
            });
            OutputRender(isRendering);
        });

    private static Command RenderResetCommand =>
        new Command("reset", "Stops rendering in-game debug overlay")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var isRendering = braidGame.Process.InjectRenderer(new()
            {
                RenderColliders = false,
            });
            OutputRender(isRendering);
        });

    private static void OutputRender(bool isRendering)
        => Console.WriteLine($"Debug overlay rendering {(isRendering ? "on" : "off")}");
}