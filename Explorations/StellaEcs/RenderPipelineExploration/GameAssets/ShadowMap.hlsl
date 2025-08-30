struct VSInput {
    float3 pos : POSITION;
};

struct VSOutput {
    float4 pos : SV_Position;
    float depth : TEXCOORD0;
};

// Vertex uniform buffer for light MVP matrix
cbuffer VSParams : register(b0, space1)
{
    float4x4 lightMVP;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos = mul(lightMVP, float4(input.pos, 1.0));
    o.depth = o.pos.z / o.pos.w; // Store depth for potential use
    return o;
}

void PSMain(VSOutput input)
{
    // Shadow map only needs depth, no color output
}
