struct VSInput {
    float3 pos : POSITION;
    float4 col : COLOR0;
};

struct VSOutput {
    float4 pos : SV_Position;
    float4 col : COLOR0;
};

cbuffer VSParams : register(b0, space1)
{
    float4x4 vp; // view-projection matrix
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos = mul(vp, float4(input.pos, 1.0));
    o.col = input.col;
    return o;
}

float4 PSMain(VSOutput input) : SV_Target
{
    return input.col;
}
