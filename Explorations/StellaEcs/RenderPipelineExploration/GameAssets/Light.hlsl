struct VSInput {
    float3 pos : POSITION;
    float3 nrm : NORMAL;
    float3 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

struct VSOutput {
    float4 pos  : SV_Position;
    float3 nrm  : NORMAL;
    float3 col  : COLOR0;
    float3 mpos : TEXCOORD0; // model-space position for point light
    float3 wpos : TEXCOORD2; // world-space position for shadows
    float2 uv   : TEXCOORD1;
};


// Vertex uniform buffer for MVP matrix (unchanged)
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
    float4x4 model;
    float4x4 modelWorld; // world-space model matrix (no camera-relative subtraction)
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos  = mul(mvp, float4(input.pos, 1.0));
    o.nrm  = normalize(input.nrm);
    o.col  = input.col;
    o.mpos = input.pos; // model space
    // modelWorld is the world-space model matrix (no camera subtraction) used by the shadow pass
    o.wpos = mul(modelWorld, float4(input.pos, 1.0)).xyz; // world space for shadow sampling
    o.uv   = input.uv;
    return o;
}

float4 PSMain(VSOutput input) : SV_Target
{
    return float4(1.0, 1.0, 1.0, 1.0);
}
