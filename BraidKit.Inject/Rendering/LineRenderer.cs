using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using static BraidKit.Inject.Rendering.ShaderHelper;

namespace BraidKit.Inject.Rendering;

internal class LineRenderer(IDirect3DDevice9 _device) : IDisposable
{
    private readonly IDirect3DVertexShader9 _lineVertexShader = _device.CreateVertexShader(CompileShader("LineShader.hlsl", "VertexShaderMain", "vs_2_0"));
    private readonly IDirect3DPixelShader9 _linePixelShader = _device.CreatePixelShader(CompileShader("LineShader.hlsl", "PixelShaderMain", "ps_2_0"));
    private readonly Primitives<LineVertex> _circleOutline = new(_device, PrimitiveType.TriangleStrip, Geometry.GetCircleOutlineTriangleStrip(1f, 1f));
    private readonly Primitives<LineVertex> _rectangleOutline = new(_device, PrimitiveType.TriangleStrip, Geometry.GetRectangleOutlineTriangleStrip(-.5f, .5f, -.5f, .5f));
    private readonly Primitives<LineVertex> _plusSignOutline = new(_device, PrimitiveType.TriangleList, Geometry.GetPlusSignTriangleList(1f));
    private const uint VS_ViewProjMtx = 0;
    private const uint VS_WorldMtx = 4;
    private const uint VS_ScaleXY_LineWidth = 8;
    private const uint PS_Color = 0;

    public void Dispose()
    {
        _lineVertexShader.Dispose();
        _linePixelShader.Dispose();
        _circleOutline.Dispose();
        _rectangleOutline.Dispose();
        _plusSignOutline.Dispose();
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

    public void RenderCircle(Vector2 center, float radius, Color4 color, float lineWidth)
    {
        RenderLine(_circleOutline, center, new(radius), color, lineWidth);
    }

    public void RenderRectangle(Vector2 center, float width, float height, Color4 color, float lineWidth, float angleDeg)
    {
        RenderLine(_rectangleOutline, center, new(width, height), color, lineWidth, angleDeg);
    }

    public void RenderPlusSign(Vector2 center, float radius, Color4 color, float lineWidth, float angleDeg)
    {
        RenderLine(_plusSignOutline, center, new(radius), color, lineWidth, angleDeg);
    }

    private void RenderLine(Primitives<LineVertex> primitives, Vector2 center, Vector2 scale, Color4 color, float lineWidth, float angleDeg = 0f)
    {
        // Set world matrix
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(angleDeg)) * Matrix4x4.CreateTranslation(center.X, center.Y, 0f));
        _device.SetVertexShaderConstant(VS_WorldMtx, worldMtx);

        // Set line style
        _device.SetVertexShaderConstant(VS_ScaleXY_LineWidth, [scale.X, scale.Y, lineWidth, 0f]);
        _device.SetPixelShaderConstant(PS_Color, [color.ToVector4()]);

        primitives.Render();
    }
}
