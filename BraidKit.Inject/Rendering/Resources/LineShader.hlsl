uniform float4x4 ViewProj;
uniform float4x4 World;
uniform float4 ScaleXY_LineWidth; // xy = Scale, z = LineWidth, w = padding

struct VS_INPUT
{
    float3 position : POSITION;
    float3 normal   : NORMAL; // Vertex offset direction
};

struct VS_OUTPUT
{
    float4 position : POSITION;
    float blend: TEXCOORD0;
};

VS_OUTPUT VertexShaderMain(VS_INPUT input)
{
    float4 pos = float4(input.position.xy * ScaleXY_LineWidth.xy - input.normal.xy * ScaleXY_LineWidth.z, input.position.z, 1.0);
    pos = mul(pos, World);
    pos = mul(pos, ViewProj);

    VS_OUTPUT output;
    output.position = pos;
    output.blend = length(input.normal);
    return output;
}

uniform float4 Color;

float4 PixelShaderMain(VS_OUTPUT input) : COLOR
{
    float blend = 4.0 * input.blend * (1.0 - input.blend); // Blend edges for a smoother look
    return float4(Color.xyz, Color.w * blend);
}
