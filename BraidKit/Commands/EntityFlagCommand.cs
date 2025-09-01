using BraidKit.Core.Game;
using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command EntityFlagCommand =>
        new Command("entity-flag", "Sets behavior flags for game entities (experimental)")
        {
            new Argument<EntityType>("entity-type").FormatEnumArgumentHelp(),
            new Argument<EntityFlags>("flag").FormatEnumArgumentHelp(),
            new Argument<BoolValue>("value").FormatEnumArgumentHelp(),
        }
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            var entityType = parseResult.GetRequiredValue<EntityType>("entity-type");
            var entityFlag = parseResult.GetRequiredValue<EntityFlags>("flag");
            var flagValue = parseResult.GetRequiredValue<BoolValue>("value") == BoolValue.True;

            var entities = braidGame
                .GetEntities()
                .Where(x => x.EntityType == entityType)
                .Where(x => x.EntityFlags.Value.HasFlag(entityFlag) != flagValue)
                .ToList();

            foreach (var entity in entities)
                entity.EntityFlags.Value ^= entityFlag; // Toggle

            Console.WriteLine($"{entityFlag} set to {flagValue} for {entities.Count} {entityType}");
        }, watermark: true);

    private enum BoolValue { False, True }
}