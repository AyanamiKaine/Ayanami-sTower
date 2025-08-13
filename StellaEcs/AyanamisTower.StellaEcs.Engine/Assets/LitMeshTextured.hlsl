// Lit textured mesh: vertex transforms and fragment samples diffuse texture

struct VSInput {
    float3 pos : POSITION;
    float3 nrm : NORMAL0;
    float2 uv  : TEXCOORD0;
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 worldPos : TEXCOORD1;
    float3 worldNrm : TEXCOORD2;
    float2 uv : TEXCOORD0;
    float3 lightPos : TEXCOORD3;
    float3 lightColor : TEXCOORD4;
    float ambient : TEXCOORD5;
    float farPlane : TEXCOORD6;
    float depthBias : TEXCOORD7;
};

// Vertex uniforms (b0, space1) to match PushVertexUniformData
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
    float3x3 m3 = (float3x3) model;
    o.worldNrm = normalize(mul(m3, input.nrm));
    o.uv = input.uv;
    o.lightPos = lightPos;
    o.lightColor = lightColor;
    o.ambient = ambient;
    o.farPlane = farPlane;
    o.depthBias = depthBias;
    return o;
}

// In MoonWorks, fragment samplers are bound via BindFragmentSamplers which maps to set/space2.
// Declare our texture/sampler in space2 so the pipeline layout matches.
Texture2D    DiffuseTex  : register(t0, space2);
SamplerState DiffuseSamp : register(s0, space2);

// Shadow map: color cube encodes linear depth in R
TextureCube  ShadowCube  : register(t1, space2);
SamplerState ShadowSamp  : register(s1, space2);

float SampleShadow(float3 lightToPoint, float currentDepth, float bias)
{
    float3 dir = normalize(lightToPoint);
    float stored = ShadowCube.Sample(ShadowSamp, dir).r;
    return currentDepth - bias <= stored ? 1.0 : 0.3;
}

float4 PSMain(VSOutput input) : SV_Target
{
    float3 N = normalize(input.worldNrm);
    float3 L = normalize(input.lightPos - input.worldPos);
    float NdotL = saturate(dot(N, L));

    float dist = distance(input.lightPos, input.worldPos) / max(1e-5, input.farPlane);
    float3 lightToPoint = input.worldPos - input.lightPos;
    float shadow = SampleShadow(lightToPoint, dist, input.depthBias);

    float3 lit = input.ambient + (NdotL * input.lightColor * shadow);
    float3 albedo = DiffuseTex.Sample(DiffuseSamp, input.uv).rgb;
    return float4(albedo * lit, 1.0);
}
