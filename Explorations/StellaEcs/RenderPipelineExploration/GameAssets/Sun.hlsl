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
    o.wpos = mul(modelWorld, float4(input.pos, 1.0)).xyz;
    o.uv   = input.uv;
    return o;
}

// Texture bindings (space2 for fragment shaders in SPIR-V)
Texture2D    DiffuseTex  : register(t0, space2);
SamplerState DiffuseSamp : register(s0, space2);

float4 PSMain(VSOutput input) : SV_Target
{
    float3 texRgb = DiffuseTex.Sample(DiffuseSamp, input.uv).rgb;
    float3 finalColor = texRgb;

    finalColor = pow(saturate(finalColor), 1.0 / 2.2);

    return float4(finalColor, 1.0);
}
