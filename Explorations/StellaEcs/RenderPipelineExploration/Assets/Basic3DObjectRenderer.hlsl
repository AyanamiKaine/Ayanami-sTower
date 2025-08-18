// Use TEXCOORD semantics for non-system values per SDL GPU HLSL guidance
struct VSInput {
    float3 pos : TEXCOORD0;
    float3 nrm : TEXCOORD1;
    float3 col : TEXCOORD2;
};

struct VSOutput {
    float4 pos : SV_Position;
    float3 nrm : TEXCOORD0;
    float3 col : TEXCOORD1;
    float3 mpos : TEXCOORD2; // world-space position for point light
};

// Vertex uniforms: model and MVP for world-space lighting
cbuffer VSParams : register(b0, space1)
{
    float4x4 model;
    float4x4 mvp;
};

// Light parameters (model space)
cbuffer LightParams : register(b0, space3)
{
    // Directional light: direction points where the light shines (from light toward scene)
    float4 Dir_Dir_Intensity;   // xyz = direction, w = intensity
    float4 Dir_Color;           // rgb = color,   a = unused

    // Point light
    float4 Pt_Pos_Range;        // xyz = position, w = range
    float4 Pt_Color_Intensity;  // rgb = color,     w = intensity
    float2 Pt_Attenuation;      // x = linear, y = quadratic
    float2 _pad;
    float  Ambient;             // scalar ambient term
    float3 _pad2;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    // Clip position
    o.pos  = mul(mvp, float4(input.pos, 1.0));

    // World-space position and normal (model has only rotation/uniform scale here)
    float4 worldPos4 = mul(model, float4(input.pos, 1.0));
    o.mpos = worldPos4.xyz; // world space position used for point light
    float3 worldNrm = mul((float3x3)model, input.nrm);
    o.nrm  = normalize(worldNrm);

    o.col  = input.col;
    return o;
}

float4 PSMain(VSOutput input) : SV_Target
{
    float3 N = normalize(input.nrm);

    // Directional light
    float3 Ld = normalize(-Dir_Dir_Intensity.xyz); // to-light vector
    float diffDir = saturate(dot(N, Ld));
    float3 dirLit = Dir_Color.rgb * (Dir_Dir_Intensity.w * diffDir);

    // Point light (world space)
    float3 toPt = Pt_Pos_Range.xyz - input.mpos;
    float dist  = length(toPt);
    float3 Lp   = (dist > 1e-5) ? toPt / dist : 0;
    float diffPt = saturate(dot(N, Lp));
    float att = 1.0 / (1.0 + Pt_Attenuation.x * dist + Pt_Attenuation.y * dist * dist);
    att *= saturate(1.0 - dist / max(Pt_Pos_Range.w, 1e-5)); // soft range cutoff
    float3 ptLit = Pt_Color_Intensity.rgb * (Pt_Color_Intensity.w * diffPt * att);

    float3 lighting = dirLit + ptLit;
    float3 finalRgb = input.col * (Ambient + lighting);
    return float4(finalRgb, 1.0);
}