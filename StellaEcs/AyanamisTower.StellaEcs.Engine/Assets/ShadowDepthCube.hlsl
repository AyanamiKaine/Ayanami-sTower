// ShadowDepthCube.hlsl
// Renders linear depth from a point light into a color cubemap face.

struct VSInput {
    float3 pos : POSITION;
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 worldPos : TEXCOORD0;
    float3 lightPos : TEXCOORD1;
    float  farPlane : TEXCOORD2;
};

// Vertex uniforms (b0, space1) to match PushVertexUniformData
cbuffer VSParams : register(b0, space1)
{
    float4x4 model;
    float4x4 viewProj;
    float3 lightPos;
    float  farPlane;
    float  depthBias; // unused in depth write, consumed in lighting
    float3 pad0;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    float4 world = mul(model, float4(input.pos, 1.0));
    o.pos = mul(viewProj, world);
    o.worldPos = world.xyz;
    o.lightPos = lightPos;
    o.farPlane = farPlane;
    return o;
}

float PSMain(VSOutput input) : SV_Target
{
    // Linear depth normalized to [0,1]
    float dist = distance(input.lightPos, input.worldPos);
    float depth = saturate(dist / max(1e-5, input.farPlane));
    return depth;
}
