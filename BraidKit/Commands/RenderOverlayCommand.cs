using BraidKit.Core;
using BraidKit.Core.Helpers;
using System.CommandLine;
using Vortice.Mathematics;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command RenderOverlayCommand =>
        new Command("render-overlay", "Renders in-game debug overlay (experimental)")
        {
            new Option<bool>("--bounds", "-b") { Description = "Show entity bounds / collision geometry", DefaultValueFactory = _ => RenderSettings.DefaultRenderEntityBounds },
            new Option<bool>("--centers", "-c") { Description = "Show entity centers", DefaultValueFactory = _ => RenderSettings.DefaultRenderEntityCenters },
            new Option<TextPosition>("--velocity", "-v") { Description = "Show Tim's velocity", DefaultValueFactory = _ => RenderSettings.DefaultRenderTimVelocity }.FormatEnumHelp("renderpos"),
            new Option<bool>("--all-entities", "-a") { Description = "Show bounds/centers for all entities", DefaultValueFactory = _ => RenderSettings.DefaultRenderAllEntities },
            new Option<float>("--line-width", "-l") { Description = "Geometry outline width", DefaultValueFactory = _ => RenderSettings.DefaultLineWidth },
            new Option<float>("--font-size", "-s") { Description = "Font size", DefaultValueFactory = _ => RenderSettings.DefaultFontSize },
            new Option<string>("--font-color", "-f") { Description = "Font color in RGBA hex format", DefaultValueFactory = _ => RenderSettings.DefaultFontColor.ToHex() },
            new Option<string>("--line-color", "-n") { Description = "Geometry line color in RGBA hex format", DefaultValueFactory = _ => RenderSettings.DefaultLineColor.ToHex() },
            RenderOverlayResetCommand,
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var renderSettings = new RenderSettings
            {
                RenderEntityBounds = parseResult.GetRequiredValue<bool>("--bounds"),
                RenderEntityCenters = parseResult.GetRequiredValue<bool>("--centers"),
                RenderTimVelocity = parseResult.GetRequiredValue<TextPosition>("--velocity"),
                RenderAllEntities = parseResult.GetRequiredValue<bool>("--all-entities"),
                LineWidth = parseResult.GetRequiredValue<float>("--line-width"),
                FontSize = parseResult.GetRequiredValue<float>("--font-size"),
                FontColor = parseResult.GetColor("--font-color", RenderSettings.DefaultFontColor),
                LineColor = parseResult.GetColor("--line-color", RenderSettings.DefaultLineColor),
            };

            var isRendering = braidGame.Process.InjectRenderer(renderSettings);
            OutputRender(isRendering);
        });

    private static Command RenderOverlayResetCommand =>
        new Command("reset", "Stops rendering in-game debug overlay")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var isRendering = braidGame.Process.InjectRenderer(RenderSettings.Off);
            OutputRender(isRendering);
        });

    private static void OutputRender(bool isRendering)
        => Console.WriteLine($"Debug overlay rendering {(isRendering ? "on" : "off")}");

    private static Color GetColor(this ParseResult parseResult, string name, Color fallbackColor)
        => ColorHelper.TryParseHex(parseResult.GetValue<string>(name) ?? "", out var parsedColor) ? parsedColor : fallbackColor;
}