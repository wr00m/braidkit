using BraidKit.Core.Helpers;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using static BraidKit.Inject.Rendering.ShaderHelper;

namespace BraidKit.Inject.Rendering;

internal class SimpleRenderer(IDirect3DDevice9 _device) : IDisposable
{
    private readonly IDirect3DVertexShader9 _textureVertexShader = _device.CreateVertexShader(CompileShader("TextureShader.hlsl", "VertexShaderMain", "vs_2_0"));
    private readonly IDirect3DPixelShader9 _texturePixelShader = _device.CreatePixelShader(CompileShader("TextureShader.hlsl", "PixelShaderMain", "ps_2_0"));
    private readonly IDirect3DTexture9 _whiteTexture = _device.LoadTexture("white.png");
    private const uint VS_ViewProjMtx = 0;
    private const uint VS_WorldMtx = 4;
    private const uint PS_Color = 0;

    public void Dispose()
    {
        _textureVertexShader.Dispose();
        _texturePixelShader.Dispose();
        _whiteTexture.Dispose();
    }

    public void Activate()
    {
        _device.VertexShader = _textureVertexShader;
        _device.PixelShader = _texturePixelShader;

        _device.SetRenderState(RenderState.Lighting, false);
        _device.SetRenderState(RenderState.ZEnable, false);
        _device.SetRenderState(RenderState.CullMode, Cull.None);
        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        _device.SetTexture(0, _whiteTexture);
    }

    public void SetViewProjectionMatrix(Matrix4x4 viewProjMtx)
    {
        _device.SetVertexShaderConstant(VS_ViewProjMtx, viewProjMtx);
    }

    public void Render(Primitives<TexturedVertex> primitives, Color? color = null)
    {
        // Set world matrix
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.Identity);
        _device.SetVertexShaderConstant(VS_WorldMtx, worldMtx);

        // Set color
        _device.SetPixelShaderConstant(PS_Color, [(color ?? ColorHelper.White).ToVector4()]);

        // Render primitives
        primitives.Render();
    }
}
