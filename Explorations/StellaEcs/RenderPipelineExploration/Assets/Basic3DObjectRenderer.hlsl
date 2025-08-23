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
    float2 uv   : TEXCOORD1;
};

// Vertex uniform buffer for MVP matrix (unchanged)
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
};

struct PointLight
{
    // Grouped to fill a 16-byte register
    float3 position;
    float  constant;

    // Grouped to fill a 16-byte register
    float3 ambient;
    float  continuous; // also called linear but its the same name as a hlsl method, so we cant use it

    // Grouped to fill a 16-byte register
    float3 diffuse;
    float  quadratic;
    
    // Grouped to fill a 16-byte register
    float3 specular;
    float  pad; // Explicit padding to make the total size a multiple of 16
};

struct DirLight
{
    float3 direction;
    float3 ambient;
    float3 diffuse;
    float3 specular;
};
/*
cbuffer DirLightProperties : register(b0, space3)
{
    DirLight dirLight;
}

#define NR_POINT_LIGHTS 120

// The array of structs is placed inside a constant buffer (cbuffer).
cbuffer PointLightsData : register(b1, space3)
{
    PointLight pointLights[NR_POINT_LIGHTS];
}
*/
VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos  = mul(mvp, float4(input.pos, 1.0));
    o.nrm  = normalize(input.nrm);
    o.col  = input.col;
    o.mpos = input.pos; // model space
    o.uv   = input.uv;
    return o;
}

// Texture bindings (space2 to avoid collision with existing spaces)
Texture2D    DiffuseTex  : register(t0, space2);
SamplerState DiffuseSamp : register(s0, space2);
// Per-draw tint (rgb*alpha, alpha) pushed from the CPU. Use a distinct space to avoid
// collisions with other constant buffers used elsewhere.
cbuffer FragmentParams : register(b0, space3)
{
    float4 tint; // rgb * alpha in xyz, alpha in w
}

float4 PSMain(VSOutput input) : SV_Target
{
    // Sample the texture and modulate by per-draw tint. The tint's alpha controls
    // the final alpha output so the CPU can animate fade-ins.
    float4 texSample = DiffuseTex.Sample(DiffuseSamp, input.uv);
    float3 rgb = texSample.rgb * tint.xyz;
    float a = tint.w; // use pushed alpha
    return float4(rgb, a);
}