using System.Numerics;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Game;

[StructLayout(LayoutKind.Explicit)]
public readonly struct PuzzlePieceData
{
    [FieldOffset(0x0)] public readonly bool Acquired;
    [FieldOffset(0x4)] public readonly int PieceIndex;
    [FieldOffset(0x8)] public readonly int Rotation;
    [FieldOffset(0xc)] public readonly Vector2 PositionInPuzzle;
    [FieldOffset(0x14)] public readonly int GroupId;
    [FieldOffset(0x18)] public readonly int WorldSubindexWherePieceLives; // Level index in world
    [FieldOffset(0x1c)] public readonly int Depth;
}
