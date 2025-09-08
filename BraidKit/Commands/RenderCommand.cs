using BraidKit.Core;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command RenderCommand =>
        new Command("render", "Renders in-game debug overlay (experimental)")
        {
            new Option<bool>("--colliders", "-c") { Description = "Show collision geometry", DefaultValueFactory = _ => RenderSettings.DefaultRenderColliders },
            new Option<bool>("--velocity", "-v") { Description = "Show Tim's velocity", DefaultValueFactory = _ => RenderSettings.DefaultRenderVelocity },
            new Option<float>("--line-width", "-l") { Description = "Geometry outline width", DefaultValueFactory = _ => RenderSettings.DefaultLineWidth },
            new Option<float>("--font-size", "-f") { Description = "Font size", DefaultValueFactory = _ => RenderSettings.DefaultFontSize },
            RenderResetCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var renderSettings = new RenderSettings
            {
                RenderColliders = parseResult.GetRequiredValue<bool>("--colliders"),
                RenderVelocity = parseResult.GetRequiredValue<bool>("--velocity"),
                LineWidth = parseResult.GetRequiredValue<float>("--line-width"),
                FontSize = parseResult.GetRequiredValue<float>("--font-size"),
            };

            var isRendering = braidGame.Process.InjectRenderer(renderSettings);
            OutputRender(isRendering);
        });

    private static Command RenderResetCommand =>
        new Command("reset", "Stops rendering in-game debug overlay")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var isRendering = braidGame.Process.InjectRenderer(new()
            {
                RenderColliders = false,
                RenderVelocity = false,
            });
            OutputRender(isRendering);
        });

    private static void OutputRender(bool isRendering)
        => Console.WriteLine($"Debug overlay rendering {(isRendering ? "on" : "off")}");
}