using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command TimTouchesCommand =>
        new Command("tim-touches", "Gets a list of entities that Tim is currently touching")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            // Ignore pieced images, because they're everywhere...
            var entities = braidGame.GetEntities().Where(x => x.EntityType != EntityType.PiecedImage).ToList();
            var tim = entities.GetTim();
            var touched = entities.Where(x => x != tim && x.RectangleIntersects(tim)).ToList();
            OutputEntities(touched);
        });

    private static void OutputEntities(List<Entity> entities)
        => entities.ForEach(x => Console.WriteLine($"{x.EntityType} at x={x.PositionX.Value:0.##} y={x.PositionY.Value:0.##}"));
}