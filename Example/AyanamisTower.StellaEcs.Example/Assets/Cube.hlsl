// Simple HLSL shader for a colored 3D cube

struct VSInput {
    float3 pos : POSITION;
    float3 col : COLOR0;
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 col : COLOR0;
};

// Vertex uniform buffer for MVP matrix
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos = mul(mvp, float4(input.pos, 1.0));
    o.col = input.col;
    return o;
}

float4 PSMain(VSOutput input) : SV_Target
{
    return float4(input.col, 1.0);
}
