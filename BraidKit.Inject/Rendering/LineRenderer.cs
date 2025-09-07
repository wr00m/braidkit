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
    private const uint VS_ViewProjMtx = 0;
    private const uint VS_WorldMtx = 4;
    private const uint VS_ScaleXY_LineWidth = 8;
    private const uint PS_Color = 0;
    public float LineWidth { get; set; } = RenderSettings.DefaultLineWidth;

    public void Dispose()
    {
        _lineVertexShader.Dispose();
        _linePixelShader.Dispose();
        _circleOutline.Dispose();
        _rectangleOutline.Dispose();
    }

    public void Activate()
    {
        _device.VertexShader = _lineVertexShader;
        _device.PixelShader = _linePixelShader;

        _device.SetRenderState(RenderState.Lighting, false);
        _device.SetRenderState(RenderState.ZEnable, false);
        _device.SetRenderState(RenderState.CullMode, Cull.None);
        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        _device.SetTexture(0, null);
    }

    public void SetViewProjectionMatrix(Matrix4x4 viewProjMtx)
    {
        _device.SetVertexShaderConstant(VS_ViewProjMtx, viewProjMtx);
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
        _device.SetVertexShaderConstant(VS_WorldMtx, worldMtx);

        // Set line style
        _device.SetVertexShaderConstant(VS_ScaleXY_LineWidth, [scale.X, scale.Y, LineWidth, 0f]);
        _device.SetPixelShaderConstant(PS_Color, [color.ToVector4()]);

        // Render vertex buffer
        _device.RenderTriangleStrip(triStrip);
    }
}
