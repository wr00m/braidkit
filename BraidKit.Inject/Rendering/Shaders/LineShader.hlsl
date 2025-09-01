uniform float4x4 ViewProj;
uniform float4x4 World;
uniform float4 ScaleXY_LineWidth; // xy = Scale, z = LineWidth, w = padding

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal   : NORMAL; // Vertex offset direction
};

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float Blend: TEXCOORD0;
};

VS_OUTPUT VertexShaderMain(VS_INPUT input)
{
    float4 pos = float4(input.Position.xy * ScaleXY_LineWidth.xy - input.Normal.xy * ScaleXY_LineWidth.z, input.Position.z, 1.0);
    pos = mul(pos, World);
    pos = mul(pos, ViewProj);

    VS_OUTPUT output;
    output.Position = pos;
    output.Blend = length(input.Normal);
    return output;
}

uniform float4 Color;

float4 PixelShaderMain(VS_OUTPUT input) : COLOR
{
    float blend = 4.0 * input.Blend * (1.0 - input.Blend); // Blend edges for a smoother look
    return float4(Color.xyz, Color.w * blend);
}
