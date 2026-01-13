using BraidKit.Core.Helpers;
using System.Numerics;
using System.Reflection;
using System.Text;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using static BraidKit.Inject.Rendering.ShaderHelper;

namespace BraidKit.Inject.Rendering;

internal class TextRenderer(IDirect3DDevice9 _device) : IDisposable
{
    private readonly IDirect3DVertexShader9 _textVertexShader = _device.CreateVertexShader(CompileShader("TextureShader.hlsl", "VertexShaderMain", "vs_2_0"));
    private readonly IDirect3DPixelShader9 _textPixelShader = _device.CreatePixelShader(CompileShader("TextureShader.hlsl", "PixelShaderMain", "ps_2_0"));
    private readonly IDirect3DTexture9 _fontTexture = _device.LoadTexture("font.png");
    private readonly FontTextureInfo _font = Assembly.GetExecutingAssembly().ReadEmbeddedJsonFile<FontTextureInfo>("font.json");
    private const uint VS_ViewProjMtx = 0;
    private const uint VS_WorldMtx = 4;
    private const uint PS_Color = 0;
    private const float _lineSpacing = .9f;

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

    public void RenderText(string text, float x, float y, HAlign alignX, VAlign alignY, float fontSize, Color fontColor, bool flipY = false)
    {
        // Set world matrix
        var fontScale = fontSize / _font.Size;
        var worldMtx = Matrix4x4.Transpose(Matrix4x4.CreateScale(fontScale, flipY ? -fontScale : fontScale, fontScale) * Matrix4x4.CreateTranslation(x, y, 0f));
        _device.SetVertexShaderConstant(VS_WorldMtx, worldMtx);

        // Set font color
        _device.SetPixelShaderConstant(PS_Color, [fontColor.ToVector4()]);

        // Get and render triangles
        var triangles = new Primitives<TexturedVertex>(_device, PrimitiveType.TriangleList, GetTriangleVertices(text, alignX, alignY), useVertexBuffer: false);
        triangles.Render();
    }

    public Vector2 GetTextSize(string text, float fontSize)
    {
        var lines = text.Split(FontTextureInfo.Newline);
        var width = lines.Select(_font.GetTextWidth).DefaultIfEmpty().Max() / _font.Size * fontSize;
        var height = lines.Length * _lineSpacing * fontSize;
        return new(width, height);
    }

    private List<TexturedVertex> GetTriangleVertices(string text, HAlign alignX, VAlign alignY)
    {
        var result = new List<TexturedVertex>(text.Length);
        var divWidth = 1f / _font.Width;
        var divHeight = 1f / _font.Height;
        var lines = text.Split(FontTextureInfo.Newline);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            var x = alignX switch
            {
                HAlign.Left => 0f,
                HAlign.Center => -.5f * _font.GetTextWidth(line),
                HAlign.Right => -_font.GetTextWidth(line),
                _ => throw new ArgumentOutOfRangeException(nameof(alignX), alignX, null),
            };

            var y = alignY switch
            {
                VAlign.Top => (i + 1f) * _font.Size * _lineSpacing,
                VAlign.Middle => (i + 1f - .5f * lines.Length) * _font.Size * _lineSpacing,
                VAlign.Bottom => (i + 1f - lines.Length) * _font.Size * _lineSpacing,
                _ => throw new ArgumentOutOfRangeException(nameof(alignX), alignX, null),
            };

            foreach (var @char in line)
            {
                const char replacementChar = '�';
                if (!_font.Characters.TryGetValue(@char, out var c) && !_font.Characters.TryGetValue(replacementChar, out c))
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

    /// <summary>Attempts to fit text to width by adding line breaks</summary>
    /// <param name="textSize">Actual size of returned text</param>
    public string LineBreakToFitMaxWidth(string text, float fontSize, float maxWidth, out Vector2 textSize)
    {
        // Early exit if text fits as-is
        textSize = GetTextSize(text, fontSize);
        if (textSize.X <= maxWidth)
            return text;

        // Split the text by existing line breaks
        var inputLines = text.Split(FontTextureInfo.Newline);
        var outputLines = new List<string>();

        foreach (var inputLine in inputLines)
        {
            var words = inputLine.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                var candidate = currentLine.Length == 0 ? word : (currentLine + " " + word);

                // If adding the word doesn't exceed maxWidth, append it
                if (GetTextSize(candidate, fontSize).X <= maxWidth)
                {
                    currentLine.Clear();
                    currentLine.Append(candidate);
                }
                else
                {
                    // Add the current line to output if it's not empty
                    if (currentLine.Length > 0)
                        outputLines.Add(currentLine.ToString());

                    // Start a new line with the word
                    // If the word itself is longer than maxWidth, it will still be placed on a single line
                    // This ensures no infinite loops or crashes occur
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }

            // Add the last output line for this input line
            if (currentLine.Length > 0)
                outputLines.Add(currentLine.ToString());
        }

        var result = string.Join(FontTextureInfo.Newline, outputLines);
        textSize = GetTextSize(result, fontSize);
        return result;
    }
}

public enum VAlign { Top, Middle, Bottom }
public enum HAlign { Left, Center, Right }