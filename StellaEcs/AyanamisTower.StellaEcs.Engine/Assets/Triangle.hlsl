// Simple HLSL shaders for a colored triangle

struct VSInput {
    float2 pos : POSITION;
    float3 col : COLOR0;
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 col : COLOR0;
};

// Vertex uniform buffer for rotation (cos,sin) and offset (x,y)
// Bind uniforms to space1 so MoonWorks' PushVertexUniformData(slot=0) maps correctly
cbuffer VSParams : register(b0, space1)
{
    float4 data; // xy = cos,sin; zw = offset
};

VSOutput VSMain(VSInput input)
{
    float2 p = input.pos;
    float c = data.x;
    float s = data.y;
    float2 uOffset = data.zw;
    float2 pr = float2(p.x * c - p.y * s, p.x * s + p.y * c) + uOffset;

    VSOutput o;
    o.pos = float4(pr, 0.0, 1.0);
    o.col = input.col;
    return o;
}

float4 PSMain(VSOutput input) : SV_Target
{
    return float4(input.col, 1.0);
}
