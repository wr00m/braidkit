using BraidKit.Core.MemoryAccess;
using BraidKit.Core.Network;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BraidKit.Core.Game;

public sealed class BraidGame(Process _process, ProcessMemoryHandler _processMemoryHandler) : IDisposable
{
    public static BraidGame? GetFromOtherProcess()
    {
        var process = Process.GetProcessesByName("braid").FirstOrDefault();
        var braidGame = process != null ? new BraidGame(process, new(process.Id)) : null;
        return braidGame;
    }

    public static BraidGame GetFromCurrentProcess()
    {
        var process = Process.GetCurrentProcess();
        var braidGame = new BraidGame(process, new(process.Id));
        return braidGame;
    }

    public void Dispose()
    {
        _processMemoryHandler.Dispose();
        _process.Dispose();
    }

    public Process Process => _process;
    public ProcessMemoryHandler ProcessMemoryHandler => _processMemoryHandler;
    public bool IsSteamVersion => _process.Modules[0].ModuleMemorySize == 7663616;
    public bool IsRunning => !_process.HasExited;

    public GameValue<bool> InMainMenu { get; } = new(_processMemoryHandler, 0x5f6ecc);
    public GameValue<GameMode> GameMode { get; } = new(_processMemoryHandler, 0x5f92c0);
    public bool InPuzzleAssemblyScreen => GameMode == Game.GameMode.PuzzleAssemblyScreen;

    private static readonly byte[] _cameraEnabledBytes = [0xf3, 0x0f, 0x11];
    private static readonly byte[] _cameraDisabledBytes = [0x90, 0x90, 0x90];

    private const IntPtr _cameraUpdateXAddr = 0x4a0367;
    private bool CameraLockX
    {
        get => _processMemoryHandler.ReadBytes(_cameraUpdateXAddr, _cameraDisabledBytes.Length).SequenceEqual(_cameraDisabledBytes);
        set => _processMemoryHandler.WriteBytes(_cameraUpdateXAddr, value ? _cameraDisabledBytes : _cameraEnabledBytes);
    }

    private const IntPtr _cameraUpdateYAddr = 0x4a036f;
    private bool CameraLockY
    {
        get => _processMemoryHandler.ReadBytes(_cameraUpdateYAddr, _cameraDisabledBytes.Length).SequenceEqual(_cameraDisabledBytes);
        set => _processMemoryHandler.WriteBytes(_cameraUpdateYAddr, value ? _cameraDisabledBytes : _cameraEnabledBytes);
    }

    public bool CameraLock
    {
        get => CameraLockX || CameraLockY;
        set => CameraLockX = CameraLockY = value;
    }

    public GameValue<Vector2> CameraPosition { get; } = new(_processMemoryHandler, 0x5f6abc);
    public GameValue<float> CameraPositionX { get; } = new(_processMemoryHandler, 0x5f6abc);
    public GameValue<float> CameraPositionY { get; } = new(_processMemoryHandler, 0x5f6ac0);
    public GameValue<int> IdealWidth { get; } = new(_processMemoryHandler, 0x5f6a90, 1280);
    public GameValue<int> IdealHeight { get; } = new(_processMemoryHandler, 0x5f6a94, 720);
    public GameValue<int> ScreenWidth { get; } = new(_processMemoryHandler, 0x5f6a98); // desired_aperture_width
    public GameValue<int> ScreenHeight { get; } = new(_processMemoryHandler, 0x5f6a9c); // desired_aperture_height
    private GameValue<int> SpeedrunFlags { get; } = new(_processMemoryHandler, 0x5f9428);
    private GameValue<uint> SpeedrunNumFrames { get; } = new(_processMemoryHandler, 0x5f9434);

    public bool IsSpeedrunModeActive => SpeedrunFlags != 0;
    public int? SpeedrunFrameIndex => IsSpeedrunModeActive ? (int)SpeedrunNumFrames.Value : null;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LaunchSpeedrunDelegate(int world, int level, int runIndex);
    private static readonly LaunchSpeedrunDelegate _launchSpeedrun = Marshal.GetDelegateForFunctionPointer<LaunchSpeedrunDelegate>(0x4c3dc0);
    public void LaunchFullGameSpeedrun() => _launchSpeedrun(0, 0, 5);

    public float Zoom
    {
        get => IdealWidth.DefaultValue / (float)IdealWidth.Value;
        set
        {
            IdealWidth.Value = (int)Math.Round(IdealWidth.DefaultValue / value);
            IdealHeight.Value = (int)Math.Round(IdealHeight.DefaultValue / value);

            // TODO: Fix issues with the in-game menu caused by zooming
            const IntPtr _updatePostprocessFxAddr = 0x004f8960;
            _processMemoryHandler.CallFunction(_updatePostprocessFxAddr);
        }
    }

    private GameValue<double> TimRunSpeed { get; } = new(_processMemoryHandler, 0x5f6f08, 200);
    private GameValue<double> TimAirSpeed { get; } = new(_processMemoryHandler, 0x5f6f30, 200);
    private GameValue<double> TimClimbSpeed { get; } = new(_processMemoryHandler, 0x5f6f20, 173.3);
    public float TimSpeedMultiplier
    {
        get => (float)(TimRunSpeed.Value / TimRunSpeed.DefaultValue);
        set
        {
            TimRunSpeed.Value = TimRunSpeed.DefaultValue * value;
            TimAirSpeed.Value = TimAirSpeed.DefaultValue * value;
            TimClimbSpeed.Value = TimClimbSpeed.DefaultValue * value;
        }
    }

    private GameValue<double> TimJumpSpeed { get; } = new(_processMemoryHandler, 0x5f6f28, 360);
    public float TimJumpMultiplier
    {
        get => (float)(TimJumpSpeed.Value / TimJumpSpeed.DefaultValue);
        set => TimJumpSpeed.Value = TimJumpSpeed.DefaultValue * value;
    }

    public GameValue<LevelTransitionType> LevelTransitionType { get; } = new(_processMemoryHandler, 0x5f93c0);
    public bool TimIsEnteringDoor => LevelTransitionType == Game.LevelTransitionType.FadeOut;
    public bool TimHasTouchedFlagpole => GetDinosaurAkaGreeter()?.IsGreeterWalking ?? false;

    public GameValue<int> TimWorld { get; } = new(_processMemoryHandler, 0x5f718c);
    public GameValue<int> TimLevel { get; } = new(_processMemoryHandler, 0x5f7190);

    public GameValue<int> FrameCount { get; } = new(_processMemoryHandler, 0x5f94b0); // Frame index

    public GameValue<bool> DrawDebugInfo { get; } = new(_processMemoryHandler, 0x5f6dcf);

    private GameValue<byte> SleepPaddingHasFocusCompareValue { get; } = new(_processMemoryHandler, 0x4b51ec, 0x0);
    private const byte _invalidBool = 0x2;
    public bool FullSpeedInBackground
    {
        get => SleepPaddingHasFocusCompareValue.Value == _invalidBool;
        set => SleepPaddingHasFocusCompareValue.Value = value ? _invalidBool : SleepPaddingHasFocusCompareValue.DefaultValue;
    }

    private PointerPath DisplaySystemPointerPath { get; } = new(_processMemoryHandler, 0xb2989c, 0x4);
    public DisplaySystem DisplaySystem => new(_processMemoryHandler, DisplaySystemPointerPath.GetAddress()!.Value);

    public void AddWatermark() => _processMemoryHandler.Write(0x00507bda, 0x00579e10);

    public bool IsUsualEntityManagerActive()
    {
        var usualEntityManagerPointer = _processMemoryHandler.Read<int>(_usualEntityManagerPointerAddr);
        var currentEntityManagerPointer = _processMemoryHandler.Read<int>(_currentEntityManagerPointerAddr);
        return currentEntityManagerPointer != default && currentEntityManagerPointer == usualEntityManagerPointer;
    }

    private const int _currentEntityManagerPointerAddr = 0x5f6de8; // This pointer can temporarily change depending on what's being rendered
    private const int _usualEntityManagerPointerAddr = 0x5f6dec; // This pointer is stable
    public List<Entity> GetEntities()
    {
        var entityManagerAddr = _processMemoryHandler.Read<int>(_usualEntityManagerPointerAddr);
        var entityAddrs = new AutoArray<int>(_processMemoryHandler, entityManagerAddr + 0xc).GetAllItems();
        var entities = entityAddrs.Select(x => new Entity(_processMemoryHandler, x)).ToList();
        return entities;
    }

    private IEnumerable<Entity> GetEntitiesByPortableType(PortableTypeAddr portableTypeAddr)
    {
        var portableType = new PortableType(_processMemoryHandler, portableTypeAddr);
        var linkedListArrayAddr = new PointerPath(_processMemoryHandler, _usualEntityManagerPointerAddr, 0x30).GetAddress();
        if (linkedListArrayAddr is null)
            return [];

        var linkedListArray = new CArray(linkedListArrayAddr.Value, MemoryAccess.LinkedList<IntPtr>.StructSize);
        var linkedListAddr = linkedListArray.GetItemAddr(portableType.SerialNumber);
        var linkedList = new MemoryAccess.LinkedList<IntPtr>(_processMemoryHandler, linkedListAddr);
        var entities = linkedList.GetItems().Select(entityAddr => new Entity(_processMemoryHandler, entityAddr));
        return entities;
    }

    public bool TryGetTim([NotNullWhen(true)] out Entity? entity) => (entity = GetTimOrNull()) != null;
    public Entity? GetTimOrNull() => GetEntitiesByPortableType(PortableTypeAddr.Guy).FirstOrDefault();
    public Entity GetTim() => GetTimOrNull() ?? throw new Exception("Where's Tim?");
    public GreeterEntity? GetDinosaurAkaGreeter() => GetEntitiesByPortableType(PortableTypeAddr.Greeter).FirstOrDefault()?.AsGreeter();
    public List<Entity> GetPuzzleFrames() => [.. GetEntitiesByPortableType(PortableTypeAddr.PuzzleFrame)];
    public GameValue<SpriteAnimationSet> TimSpriteAnimationSet { get; } = new(_processMemoryHandler, _processMemoryHandler.Read<IntPtr>(0x5f71e4));

    /// <summary>Fades to black during level transitions</summary>
    public GameValue<Vector4> EntityVertexColorScale { get; } = new(_processMemoryHandler, 0x5f7070);

    private const IntPtr _gameGlobalsAddr = 0x5f6990;
    public CampaignState UsualCampaignState => new(_processMemoryHandler, _gameGlobalsAddr + 0x890); // Non-speedrun state
    public CampaignState SpeedrunCampaignState => new(_processMemoryHandler, _gameGlobalsAddr + 0x18e0);
    public CampaignState CurrentCampaignState => IsSpeedrunModeActive ? SpeedrunCampaignState : UsualCampaignState;

    public bool TryGetTimSprite(EntitySnapshot entity, out RectangleF world, out RectangleF uv, out IntPtr textureMapAddr)
    {
        var animationSet = TimSpriteAnimationSet.Value;
        if (entity.AnimationIndex < 0 || entity.AnimationIndex >= animationSet.NumAnimations)
        {
            world = RectangleF.Empty;
            uv = RectangleF.Empty;
            textureMapAddr = IntPtr.Zero;
            return false;
        }

        var animationAddr = _processMemoryHandler.Read<IntPtr>(animationSet.AnimationArray + sizeof(int) * entity.AnimationIndex);
        var animation = _processMemoryHandler.Read<SpriteAnimation>(animationAddr);

        // TODO: Frame index seems a bit off compared to original Tim entity -- is it not calculated correctly?
        var normTime = animation.Duration > 0f ? entity.AnimationTime / animation.Duration : 0f; // Normalize time
        normTime = (normTime % 1f + 1f) % 1f; // Repeat time [0..1)
        var frameIndex = (int)(normTime * animation.NumFrames); // [0..NumFrames-1]
        var frame = _processMemoryHandler.Read<SpriteAnimationFrame>(animation.FrameArray + 0x24 * frameIndex);

        textureMapAddr = animationSet.TextureMap;
        var textureMap = _processMemoryHandler.Read<TextureMap>(textureMapAddr);
        var divSize = new Vector2(1f / textureMap.Width, 1f / textureMap.Height);

        var pos = entity.Position;
        var originX = frame.OriginOffset.X;
        if (entity.FacingLeft)
            originX = frame.Width - originX;
        var uv0 = new Vector2(frame.X0, frame.Y0) * divSize;
        var uvSize = new Vector2(frame.Width, frame.Height) * divSize;
        world = new(pos.X - originX, pos.Y - frame.OriginOffset.Y, frame.Width, frame.Height);
        uv = new(uv0.X, uv0.Y, uvSize.X, uvSize.Y);

        if (entity.FacingLeft)
            uv = new(uv.Right, uv.Y, -uv.Width, uv.Height); // Flip x-axis (mirrored with negative width)

        return true;
    }

    public bool TryCreateTimGameQuad(EntitySnapshot entity, out GameQuad result, uint scaleColor = 0xffffffff)
    {
        if (!TryGetTimSprite(entity, out var world, out var uv, out var textureMapAddr))
        {
            result = default;
            return false;
        }

        var camPos = CameraPosition.Value;
        Vector3 WorldToView(Vector2 pos) => new(pos - camPos, .5f);

        result = new()
        {
            RenderPriority = .1f, // Render behind normal Tim
            Parallax = 1,
            PortableId = -1,
            Pos0 = WorldToView(new(world.Left, world.Bottom)),
            Pos1 = WorldToView(new(world.Right, world.Bottom)),
            Pos2 = WorldToView(new(world.Right, world.Top)),
            Pos3 = WorldToView(new(world.Left, world.Top)),
            UV0 = new(uv.Left, uv.Bottom),
            UV1 = new(uv.Right, uv.Bottom),
            UV2 = new(uv.Right, uv.Top),
            UV3 = new(uv.Left, uv.Top),
            TextureMap = textureMapAddr,
            PiecedImage = IntPtr.Zero,
            Flags = default,
            ScaleColor = scaleColor,
            AddColor = default,
            CompandScale = 1,
        };

        return true;
    }

    public GameValue<int> NumGameQuads { get; } = new(_processMemoryHandler, 0x63f5f0);

    public void AddGameQuad(GameQuad gameQuad)
    {
        var numGameQuads = NumGameQuads.Value;

        const int maxGameQuads = 40000;
        if (numGameQuads >= maxGameQuads)
            return;

        var gameQuadAddr = GetGameQuadAddress(numGameQuads);
        _processMemoryHandler.Write(gameQuadAddr, gameQuad);

        var gameQuadPointerAddr = GetGameQuadPointerAddress(numGameQuads);
        _processMemoryHandler.Write(gameQuadPointerAddr, gameQuadAddr);

        NumGameQuads.Value = numGameQuads + 1;
    }

    private static IntPtr GetGameQuadAddress(int index)
    {
        const int _gameQuadBytes = 0x74;
        const IntPtr gameQuadArrayAddr = 0x666748;
        var gameQuadAddr = gameQuadArrayAddr + _gameQuadBytes * index;
        return gameQuadAddr;
    }

    private static IntPtr GetGameQuadPointerAddress(int index)
    {
        const IntPtr gameQuadPointerArrayAddr = 0x63f610;
        var gameQuadPointerAddr = gameQuadPointerArrayAddr + sizeof(int) * index;
        return gameQuadPointerAddr;
    }
}

// Note: These values are sometimes used as bitwise flags, not sure what to make of that
public enum LevelTransitionType
{
    /// <summary>No fade in/out</summary>
    None = 0,
    /// <summary>Level fade-out when Tim enters a door</summary>
    FadeOut = 1,
    /// <summary>No idea what this is for</summary>
    Unknown = 2,
    /// <summary>Level fade-in when visited for the first time (non-speedrun mode)</summary>
    FadeInSlow = 3,
    /// <summary>Level fade-in when already visited, or speedrun mode is active</summary>
    FadeInFast = 4,
    /// <summary>Not sure how this differs from <see cref="FadeInFast"/></summary>
    FadeInWorld6Clouds = 5,
}

public enum GameMode
{
    TitleScreen = 0,
    PuzzleAssemblyScreen = 1,
    Game = 2,
    CallServer = 3,
    EditAnimations = 4,
}