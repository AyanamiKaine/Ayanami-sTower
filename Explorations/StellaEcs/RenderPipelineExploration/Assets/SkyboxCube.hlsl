struct VSInput {
    float3 pos : POSITION;
};

struct VSOutput {
    float4 pos  : SV_Position;
    float3 mpos : TEXCOORD0; // model-space position for cube direction
};

// Match existing space convention: VSParams at b0, space1
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
};

// Simple skybox VS: use provided MVP (we translate to camera already in C#)
VSOutput VSMain(VSInput i)
{
    VSOutput o;
    o.pos = mul(mvp, float4(i.pos, 1.0));
    o.mpos = i.pos; // model-space direction
    return o;
}

// TextureCube bindings (space2 like other textures)
TextureCube    SkyCube  : register(t0, space2);
SamplerState   SkySamp  : register(s0, space2);

float4 PSMain(VSOutput input) : SV_Target
{
    float3 dir = normalize(input.mpos);
    float3 col = SkyCube.Sample(SkySamp, dir).rgb;
    return float4(col, 1.0);
}
