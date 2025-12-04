using BraidKit.Core.MemoryAccess;

namespace BraidKit.Core.Game;

public class CampaignState(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr)
{
    private const int _puzzleWorldCount = 5;
    private const int _pieceCountPerPuzzle = 12;

    public int CountAcquiredPuzzlePieces() => EnumeratePuzzlePieceAcquiredAddrs().Sum(addr => _processMemoryHandler.Read<bool>(addr) ? 1 : 0);

    public void ResetPieces()
    {
        // Note: Pieces in current level don't reset, but maybe that's good since we don't want to reset pieces during level fadeout
        foreach (var puzzlePieceAddr in EnumeratePuzzlePieceAcquiredAddrs())
            _processMemoryHandler.Write(puzzlePieceAddr, false);
    }

    private IntPtr GetPuzzlePieceAddr(int world, int piece)
    {
        const IntPtr worldPuzzlePieceOffset = 0x18c;
        const IntPtr individualPuzzlePieceOffset = 0x20;
        IntPtr initialPuzzlePieceAddr = _addr + 0x364;

        if (world < 0 || world >= _puzzleWorldCount || piece < 0 || piece >= _pieceCountPerPuzzle)
            return IntPtr.Zero;

        var puzzlePieceAddr = initialPuzzlePieceAddr + worldPuzzlePieceOffset * world + individualPuzzlePieceOffset * piece;
        return puzzlePieceAddr;
    }

    private IEnumerable<IntPtr> EnumeratePuzzlePieceAcquiredAddrs()
    {
        for (int world = 0; world < _puzzleWorldCount; world++)
            for (int piece = 0; piece < _pieceCountPerPuzzle; piece++)
                yield return GetPuzzlePieceAddr(world, piece);
    }
}
