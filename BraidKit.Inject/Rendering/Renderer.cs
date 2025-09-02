using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using Vortice.D3DCompiler;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace BraidKit.Inject.Rendering;

// TODO: Do vertex buffers, shaders, and device survive fullscreen toggle? If so, move device from methods to ctor
internal class Renderer(BraidGame _braidGame) : IDisposable
{
    private IDirect3DVertexShader9? _lineVertexShader;
    private IDirect3DPixelShader9? _linePixelShader;
    private TriangleStrip? _circle;
    private TriangleStrip? _rectangle;
    public float LineWidth { get; set; } = RenderSettings.DefaultLineWidth;

    public void Dispose()
    {
        _circle?.Dispose();
        _rectangle?.Dispose();
    }

    private static ReadOnlySpan<byte> CompileShader(string filename, string entryPoint, string profile)
    {
        var shaderSource = Assembly.GetExecutingAssembly().ReadEmbeddedResourceFile(filename);
        var result = Compiler.Compile(shaderSource, entryPoint, filename, profile, out var compiledBlob, out var errorBlob);
        if (result.Failure)
            Log($"Shader compilation failed: {errorBlob?.AsString() ?? "Unknown error"}");
        return compiledBlob.AsSpan();
    }

    public void RenderCollisionGeometries(IDirect3DDevice9 device)
    {
        _lineVertexShader ??= device.CreateVertexShader(CompileShader("LineShader.hlsl", "VertexShaderMain", "vs_2_0"));
        _linePixelShader ??= device.CreatePixelShader(CompileShader("LineShader.hlsl", "PixelShaderMain", "ps_2_0"));

        // Set render state
        device.VertexShader = _lineVertexShader;
        device.PixelShader = _linePixelShader;
        device.SetRenderState(RenderState.Lighting, false);
        device.SetRenderState(RenderState.ZEnable, false); // TODO: Does this make any difference?
        device.SetRenderState(RenderState.CullMode, Cull.None); // TODO: Does this make any difference?
        device.SetRenderState(RenderState.FogEnable, false); // TODO: Does this make any difference?
        device.SetRenderState(RenderState.AlphaBlendEnable, true);
        device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        device.SetTexture(0, null);
        device.SetTextureStageState(0, TextureStage.ColorOperation, (int)TextureOperation.SelectArg1);
        device.SetTextureStageState(0, TextureStage.ColorArg1, (int)TextureArgument.Diffuse);

        // Set view + projection matrix
        var viewProjMtx = Matrix4x4.Transpose(Matrix4x4.CreateOrthographicOffCenter(
            _braidGame.CameraPositionX,
            _braidGame.CameraPositionX + _braidGame.IdealWidth,
            _braidGame.CameraPositionY,
            _braidGame.CameraPositionY + _braidGame.IdealHeight,
            0f,
            1f));
        device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_ViewProjMtx, viewProjMtx);

        var entities = _braidGame.GetEntities();
        foreach (var entity in entities)
            RenderCollisionGeometry(device, entity);
    }

    private void RenderCollisionGeometry(IDirect3DDevice9 device, Entity entity)
    {
        var color = entity.EntityType.Value switch
        {
            EntityType.Guy => new Color4(1f, 1f, 1f, 1f),
            EntityType.Claw => new Color(0f, 1f, 0f, 1f),
            EntityType.Bullet or
            EntityType.Cannon or
            EntityType.Chandelier or
            EntityType.ChandelierHook or
            EntityType.Cloud or
            EntityType.Door or
            EntityType.Flagpole or
            EntityType.Floor or
            EntityType.Gate or
            EntityType.GunBoss or
            EntityType.Key or
            EntityType.Mimic or
            EntityType.Monstar or
            EntityType.Platform or
            EntityType.Prince or
            EntityType.Princess or
            EntityType.PuzzleFrame or
            EntityType.PuzzlePiece or
            EntityType.Ring or
            EntityType.SpecialItem => entity.IsMonster() ? new Color4(1f, 0f, 0f, 1f) : new Color4(0f, 0f, 1f, 1f),
            _ => (Color4?)null,
        };

        if (color is null)
            return;

        if (entity.IsRectangular())
            RenderRectangle(device, entity.Center, entity.Width, entity.Height, color.Value, entity.Theta);
        else
            RenderCircle(device, entity.Center, entity.GetCircleRadius(), color.Value);
    }

    [MemberNotNull(nameof(_circle))]
    private void RenderCircle(IDirect3DDevice9 device, Vector2 center, float radius, Color4 color)
    {
        _circle ??= new(device, Geometry.GetCircleTriangleStrip(1f, 1f));
        RenderTriangleStrip(device, _circle, center, new(radius, radius), color);
    }

    [MemberNotNull(nameof(_rectangle))]
    private void RenderRectangle(IDirect3DDevice9 device, Vector2 center, float width, float height, Color4 color, float angleDeg)
    {
        _rectangle ??= new(device, Geometry.GetRectangleTriangleStrip(-.5f, .5f, -.5f, .5f));
        RenderTriangleStrip(device, _rectangle, center, new(width, height), color, angleDeg);
    }

    private void RenderTriangleStrip(IDirect3DDevice9 device, TriangleStrip triStrip, Vector2 center, Vector2 scale, Color4 color, float angleDeg = 0f)
    {
        // Set world matrix
        const float _degToRad = 0.017453292519943295f;
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.CreateRotationZ(angleDeg * _degToRad) * Matrix4x4.CreateTranslation(center.X, center.Y, 0f));
        device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_WorldMtx, worldMtx);

        // Set line style
        device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_ScaleXY_LineWidth, [scale.X, scale.Y, LineWidth, 0f]);
        device.SetPixelShaderConstant((uint)LineShaderUniformRegister.PS_Color, [color.ToVector4()]);

        // Render vertex buffer
        device.SetStreamSource(0, triStrip._vertexBuffer, 0, triStrip.Stride);
        device.VertexFormat = triStrip.VertexFormat;
        device.DrawPrimitive(PrimitiveType.TriangleStrip, 0, triStrip.PrimitiveCount);
    }

    private enum LineShaderUniformRegister : uint
    {
        VS_ViewProjMtx = 0,
        VS_WorldMtx = 4,
        VS_ScaleXY_LineWidth = 8,
        PS_Color = 0,
    }

    private static void Log(params string[] lines) => File.AppendAllLines("braidkit.log", lines);
}
