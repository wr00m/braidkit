using System.CommandLine;

namespace BraidKit.Commands;

internal static partial class Commands
{
    private static Command ResetPiecesCommand =>
        new Command("reset-pieces", "Resets all puzzle pieces for current save")
        .SetBraidGameAction((braidGame, parseResult) =>
        {
            braidGame.UsualCampaignState.ResetPieces();
        });
}