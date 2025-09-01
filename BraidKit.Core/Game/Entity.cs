using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using BraidKit.Core.MemoryAccess;

namespace BraidKit.Core.Game;

public class Entity(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr)
{
    public IntPtr Addr => _addr;
    public GameValue<EntityType> EntityType { get; } = new(_processMemoryHandler, _addr);
    public GameValue<EntityFlags> EntityFlags { get; } = new(_processMemoryHandler, _addr + 0x10);
    public GameValue<float> PositionX { get; } = new(_processMemoryHandler, _addr + 0x14);
    public GameValue<float> PositionY { get; } = new(_processMemoryHandler, _addr + 0x18);
    public GameValue<float> VelocityX { get; } = new(_processMemoryHandler, _addr + 0x1c);
    public GameValue<float> VelocityY { get; } = new(_processMemoryHandler, _addr + 0x20);
    public GameValue<float> Width { get; } = new(_processMemoryHandler, _addr + 0x24);
    public GameValue<float> Height { get; } = new(_processMemoryHandler, _addr + 0x28);
    public GameValue<float> Theta { get; } = new(_processMemoryHandler, _addr + 0x38); // Rotation in degrees
    private GameValue<int> SupportedByPortableId { get; } = new(_processMemoryHandler, _addr + 0x78);
    public Vector2 Center => new(PositionX, PositionY + Height * .5f);
    public Vector2 Size => new(Width, Height);

    public void DetachFromGround() => SupportedByPortableId.Value = 0;

    // TODO: This doesn't handle rotated/scaled hitboxes -- maybe we should use Intersects(other) instead but that only works within braid.exe
    public bool RectangleIntersects(Entity other, float scale = 1.05f) => GetRectangle(scale).IntersectsWith(other.GetRectangle(scale));
    private RectangleF GetRectangle(float scale = 1f)
    {
        var c = Center;
        var w = Width * scale;
        var h = Height * scale;
        return new(c.X - w * .5f, c.Y + h * .5f, w, h);
    }

    // TODO: Game functions should be called via ProcessMemoryHandler and support both current process and other process
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate float GetCircleRadiusDelegate(IntPtr entityAddr);
    private static readonly GetCircleRadiusDelegate _getCircleRadius = Marshal.GetDelegateForFunctionPointer<GetCircleRadiusDelegate>(0x4b5b10);
    public float GetCircleRadius() => _getCircleRadius(_addr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool IsRectangularDelegate(IntPtr entityAddr);
    private static readonly IsRectangularDelegate _isRectangular = Marshal.GetDelegateForFunctionPointer<IsRectangularDelegate>(0x48c460);
    public bool IsRectangular() => _isRectangular(_addr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool IsMonsterDelegate(IntPtr entityAddr);
    private static readonly IsMonsterDelegate _isMonster = Marshal.GetDelegateForFunctionPointer<IsMonsterDelegate>(0x483570);
    public bool IsMonster() => _isMonster(_addr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool CircleEntityIntersectsRectangularEntityDelegate(IntPtr entity1Addr, IntPtr entity2Addr, out Vector2 overlap, out Vector2 collisionNormal, out Vector2 collisionPoint, float extraRadius, float verticalOffset);
    private static readonly CircleEntityIntersectsRectangularEntityDelegate _circleEntityIntersectsRectangularEntity = Marshal.GetDelegateForFunctionPointer<CircleEntityIntersectsRectangularEntityDelegate>(0x4df010);
    public bool Intersects(Entity other) => _circleEntityIntersectsRectangularEntity(_addr, other.Addr, out var _, out var _, out var _, 0f, 0f);

    public FlagpoleEntity? AsFlagpole() => EntityType == Game.EntityType.Flagpole ? new(_processMemoryHandler, _addr) : null;
    public GreeterEntity? AsGreeter() => EntityType == Game.EntityType.Greeter ? new(_processMemoryHandler, _addr) : null;
}

public class FlagpoleEntity(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr) : Entity(_processMemoryHandler, _addr)
{
    public GameValue<float> FlagpoleTimeOfActivation { get; } = new(_processMemoryHandler, _addr + 0x58);
    public bool IsFlagpoleActivated => FlagpoleTimeOfActivation >= 0f;
}

public class GreeterEntity(ProcessMemoryHandler _processMemoryHandler, IntPtr _addr) : Entity(_processMemoryHandler, _addr)
{
    public GameValue<bool> IsGreeterWalking { get; } = new(_processMemoryHandler, _addr + 0x50);
}

/// <summary>Flag names are best guesses, take with a grain of salt</summary>
[Flags]
public enum EntityFlags : uint
{
    // TODO: Identify more entity flags
    Hidden = 0x1,
    Immovable = 0x4,
    CollidablePlatformOrProjectile = 0x10,
    CollidableTerrain = 0x20,
    GreenGlow = 0x40,
    Climbable = 0x80,
    NoGravity = 0x4000,
    RectangularCollider = 0x8000,
    PurpleGlow = 0x10000,
    // TODO: 0x2000000u is apparently something
}

/// <summary>Hex values are vtable memory addresses</summary>
public enum EntityType
{
    Booster = 0x0057858c,
    Bullet = 0x005785b8,
    CameraControl = 0x00578808,
    Cannon = 0x00578918,
    Chandelier = 0x00578b08,
    ChandelierHook = 0x00578dac,
    Claw = 0x00578ddc, // Piranha plant
    Cloud = 0x00578e18,
    ConstellationLine = 0x005791b4,
    Debris = 0x005791f4,
    Door = 0x00579214,
    Flagpole = 0x0057966c,
    Floor = 0x0057969c,
    Gate = 0x00579c3c, // Gate with keyhole
    Greeter = 0x00579ca8, // "The Princess is in another castle" dinosaur
    GunBoss = 0x00579cf0,
    Guy = 0x00579e60, // Tim
    IconBlock = 0x0057a118,
    IssuedSound = 0x0057a140,
    Key = 0x0057a174,
    KillerPan = 0x0057a1f8,
    Marker = 0x0057bf88,
    Mimic = 0x0057bfd0, // Rabbit
    Monstar = 0x0057c080, // Goomba
    Paradigm = 0x0057c340,
    Particle = 0x0057c370,
    Particles = 0x0057c3a0,
    PiecedImage = 0x0057c3f4,
    PiecedImageOrigin = 0x0057c498,
    Platform = 0x0057c4d8,
    Prince = 0x0057c604, // Knight in level 1-1
    Princess = 0x005800a8,
    PuzzleFrame = 0x0057ca10,
    PuzzlePiece = 0x0057cacc,
    Rainmaker = 0x0057cb18,
    ReverseEffect = 0x0057cb48,
    Ring = 0x0057cb88,
    Rumble = 0x0057cbc8,
    Shine = 0x0057d93c,
    SpecialItem = 0x0057da28,
    Star = 0x0057db68,
    StoryPage = 0x0057dc8c,
    Sun = 0x0057dd1c,
    Surface = 0x0057dd40,
    Switch = 0x0057dd70, // Lever
    Text = 0x0057964c,
    Tiler = 0x0057de78,
}

public static class EntityHelper
{
    public static Entity GetTim(this IEnumerable<Entity> entities) => entities.FirstOrDefault(x => x.EntityType == EntityType.Guy) ?? throw new Exception("Tim not found");
}