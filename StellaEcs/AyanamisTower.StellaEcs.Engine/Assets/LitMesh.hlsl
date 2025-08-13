// Basic point light Lambert shading for 3D meshes with normals

struct VSInput {
    float3 pos : POSITION;
    float3 nrm : NORMAL0;
    float3 col : COLOR0; // albedo
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 worldPos : TEXCOORD0;
    float3 worldNrm : TEXCOORD1;
    float3 col : COLOR0;
    float3 lightPos : TEXCOORD2;
    float3 lightColor : TEXCOORD3;
    float ambient : TEXCOORD4;
    float farPlane : TEXCOORD5;
    float depthBias : TEXCOORD6;
};

// Vertex uniforms: single slot (b0, space1) containing MVP, Model, and light data
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
    float4x4 model;
    float3 lightPos;
    float  ambient;
    float3 lightColor;
    float  farPlane;
    float  depthBias;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    float4 world = mul(model, float4(input.pos, 1.0));
    o.pos = mul(mvp, float4(input.pos, 1.0));
    o.worldPos = world.xyz;
    // Assume uniform scale for simplicity
    float3x3 m3 = (float3x3) model;
    o.worldNrm = normalize(mul(m3, input.nrm));
    o.col = input.col;
    o.lightPos = lightPos;
    o.lightColor = lightColor;
    o.ambient = ambient;
    o.farPlane = farPlane;
    o.depthBias = depthBias;
    return o;
}

// Shadow map: color cube encodes linear depth in R (space2)
// Note: bind at t0/s0 to avoid gaps since this pipeline has no diffuse texture.
TextureCube ShadowCube : register(t0, space2);
SamplerState ShadowSamp : register(s0, space2);

float SampleShadow(float3 lightToPoint, float currentDepth, float bias)
{
    // Normalize direction and sample stored depth
    float3 dir = normalize(lightToPoint);
    float stored = ShadowCube.Sample(ShadowSamp, dir).r;
    // 1 if lit, 0 if in shadow (apply bias)
    return currentDepth - bias <= stored ? 1.0 : 0.3; // 0.3 ambient in shadow
}

float4 PSMain(VSOutput input) : SV_Target
{
    float3 N = normalize(input.worldNrm);
    float3 L = normalize(input.lightPos - input.worldPos);
    float NdotL = saturate(dot(N, L));

    // Shadow factor - try both directions to debug
    float dist = distance(input.lightPos, input.worldPos) / max(1e-5, input.farPlane);
    float3 lightToPoint = input.worldPos - input.lightPos;
    float shadow = SampleShadow(lightToPoint, dist, input.depthBias);

    float3 lit = input.ambient + (NdotL * input.lightColor * shadow);
    float3 outCol = input.col * lit;
    return float4(outCol, 1.0);
}
