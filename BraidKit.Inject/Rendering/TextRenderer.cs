using BraidKit.Core;
using BraidKit.Core.Helpers;
using System.Numerics;
using System.Reflection;
using Vortice.Direct3D9;
using static BraidKit.Inject.Rendering.ShaderHelper;

namespace BraidKit.Inject.Rendering;

internal class TextRenderer(IDirect3DDevice9 _device) : IDisposable
{
    private readonly IDirect3DVertexShader9 _textVertexShader = _device.CreateVertexShader(CompileShader("TextShader.hlsl", "VertexShaderMain", "vs_2_0"));
    private readonly IDirect3DPixelShader9 _textPixelShader = _device.CreatePixelShader(CompileShader("TextShader.hlsl", "PixelShaderMain", "ps_2_0"));
    private readonly IDirect3DTexture9 _fontTexture = _device.LoadTexture("font.png");
    private readonly FontTextureInfo _font = Assembly.GetExecutingAssembly().ReadEmbeddedJsonFile<FontTextureInfo>("font.json");
    private const uint VS_ViewProjMtx = 0;
    private const uint VS_WorldMtx = 4;
    public float FontSize { get; set; } = RenderSettings.DefaultFontSize;
    public float LineSpacing { get; set; } = .9f;

    public void Dispose()
    {
        _textVertexShader.Dispose();
        _textPixelShader.Dispose();
        _fontTexture.Dispose();
    }

    public void Activate()
    {
        _device.VertexShader = _textVertexShader;
        _device.PixelShader = _textPixelShader;

        _device.SetRenderState(RenderState.Lighting, false);
        _device.SetRenderState(RenderState.ZEnable, false);
        _device.SetRenderState(RenderState.CullMode, Cull.None);
        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        _device.SetTexture(0, _fontTexture);
    }

    public void SetViewProjectionMatrix(Matrix4x4 viewProjMtx)
    {
        _device.SetVertexShaderConstant(VS_ViewProjMtx, viewProjMtx);
    }

    public void RenderText(string text, float x, float y, bool centerX, bool centerY)
    {
        // Set world matrix
        var fontScale = FontSize / _font.Size;
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.CreateScale(fontScale, -fontScale, fontScale) * Matrix4x4.CreateTranslation(x, y, 0f));
        _device.SetVertexShaderConstant(VS_WorldMtx, worldMtx);

        // Get and render triangles
        var triangles = GetTriangleVertices(text, centerX, centerY);
        _device.RenderTriangles(triangles);
    }

    private List<FontVertex> GetTriangleVertices(string text, bool centerX, bool centerY)
    {
        var result = new List<FontVertex>(text.Length);
        var divWidth = 1f / _font.Width;
        var divHeight = 1f / _font.Height;
        var lines = text.Split(FontTextureInfo.Newline);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var x = centerX ? _font.GetTextWidth(line) * -.5f : 0f;
            var y = (centerY ? (i + 1f - lines.Length * .5f) : (i + 1f)) * _font.Size * LineSpacing;

            foreach (var @char in line)
            {
                if (!_font.Characters.TryGetValue(@char, out var c))
                    continue;

                var x0 = x - c.OriginX;
                var y0 = y - c.OriginY;
                var u0 = c.X * divWidth;
                var v0 = c.Y * divHeight;

                var x1 = x - c.OriginX + c.Width;
                var y1 = y - c.OriginY;
                var u1 = (c.X + c.Width) * divWidth;
                var v1 = c.Y * divHeight;

                var x2 = x - c.OriginX;
                var y2 = y - c.OriginY + c.Height;
                var u2 = c.X * divWidth;
                var v2 = (c.Y + c.Height) * divHeight;

                var x3 = x - c.OriginX + c.Width;
                var y3 = y - c.OriginY + c.Height;
                var u3 = (c.X + c.Width) * divWidth;
                var v3 = (c.Y + c.Height) * divHeight;

                result.Add(new(x0, y0, u0, v0));
                result.Add(new(x1, y1, u1, v1));
                result.Add(new(x3, y3, u3, v3));

                result.Add(new(x0, y0, u0, v0));
                result.Add(new(x3, y3, u3, v3));
                result.Add(new(x2, y2, u2, v2));

                x += c.Advance;
            }
        }

        return result;
    }
}
