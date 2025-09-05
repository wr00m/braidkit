using BraidKit.Core;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using static BraidKit.Inject.Rendering.ShaderHelper;

namespace BraidKit.Inject.Rendering;

internal class LineRenderer(IDirect3DDevice9 _device) : IDisposable
{
    private readonly IDirect3DVertexShader9 _lineVertexShader = _device.CreateVertexShader(CompileShader("LineShader.hlsl", "VertexShaderMain", "vs_2_0"));
    private readonly IDirect3DPixelShader9 _linePixelShader = _device.CreatePixelShader(CompileShader("LineShader.hlsl", "PixelShaderMain", "ps_2_0"));
    private readonly TriangleStrip<LineVertex> _circleOutline = new(_device, Geometry.GetCircleOutlineTriangleStrip(1f, 1f));
    private readonly TriangleStrip<LineVertex> _rectangleOutline = new(_device, Geometry.GetRectangleOutlineTriangleStrip(-.5f, .5f, -.5f, .5f));
    public float LineWidth { get; set; } = RenderSettings.DefaultLineWidth;

    public void Dispose()
    {
        _lineVertexShader?.Dispose();
        _linePixelShader?.Dispose();
        _circleOutline?.Dispose();
        _rectangleOutline?.Dispose();
    }

    public void Activate()
    {
        // Set render state
        _device.VertexShader = _lineVertexShader;
        _device.PixelShader = _linePixelShader;
        _device.SetRenderState(RenderState.Lighting, false);
        _device.SetRenderState(RenderState.ZEnable, false); // TODO: Does this make any difference?
        _device.SetRenderState(RenderState.CullMode, Cull.None); // TODO: Does this make any difference?
        _device.SetRenderState(RenderState.FogEnable, false); // TODO: Does this make any difference?
        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        _device.SetTexture(0, null);
        _device.SetTextureStageState(0, TextureStage.ColorOperation, (int)TextureOperation.SelectArg1);
        _device.SetTextureStageState(0, TextureStage.ColorArg1, (int)TextureArgument.Diffuse);
    }

    public void SetViewProjectionMatrix(Matrix4x4 viewProjMtx)
    {
        _device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_ViewProjMtx, viewProjMtx);
    }

    public void RenderCircle(Vector2 center, float radius, Color4 color)
    {
        RenderLine(_circleOutline, center, new(radius, radius), color);
    }

    public void RenderRectangle(Vector2 center, float width, float height, Color4 color, float angleDeg)
    {
        RenderLine(_rectangleOutline, center, new(width, height), color, angleDeg);
    }

    private void RenderLine(TriangleStrip<LineVertex> triStrip, Vector2 center, Vector2 scale, Color4 color, float angleDeg = 0f)
    {
        // Set world matrix
        const float _degToRad = 0.017453292519943295f;
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.CreateRotationZ(angleDeg * _degToRad) * Matrix4x4.CreateTranslation(center.X, center.Y, 0f));
        _device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_WorldMtx, worldMtx);

        // Set line style
        _device.SetVertexShaderConstant((uint)LineShaderUniformRegister.VS_ScaleXY_LineWidth, [scale.X, scale.Y, LineWidth, 0f]);
        _device.SetPixelShaderConstant((uint)LineShaderUniformRegister.PS_Color, [color.ToVector4()]);

        // Render vertex buffer
        _device.SetStreamSource(0, triStrip._vertexBuffer, 0, triStrip.Stride);
        _device.VertexFormat = triStrip.VertexFormat;
        _device.DrawPrimitive(PrimitiveType.TriangleStrip, 0, triStrip.PrimitiveCount);
    }

    private enum LineShaderUniformRegister : uint
    {
        VS_ViewProjMtx = 0,
        VS_WorldMtx = 4,
        VS_ScaleXY_LineWidth = 8,
        PS_Color = 0,
    }
}
