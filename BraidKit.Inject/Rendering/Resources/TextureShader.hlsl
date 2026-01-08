uniform float4x4 ViewProj;
uniform float4x4 World;

struct VS_INPUT
{
    float3 position : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

VS_OUTPUT VertexShaderMain(VS_INPUT input)
{
    float4 pos = float4(input.position.xy, 0.0, 1.0);
    pos = mul(pos, World);
    pos = mul(pos, ViewProj);

    VS_OUTPUT output;
    output.position = pos;
    output.texcoord = input.texcoord;
    return output;
}

uniform float4 Color;
texture theTexture : register(t0);

sampler2D theSampler = sampler_state
{
    Texture = <theTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 PixelShaderMain(VS_OUTPUT input) : COLOR
{
    return tex2D(theSampler, input.texcoord) * Color;
}
