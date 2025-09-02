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

// Parameters for the pixel shader. Bound to b1 (space2) — adjust binding in host if needed.
cbuffer PSParams : register(b1, space2)
{
    float time;            // seconds
    float3 sunColor;       // base emissive tint
    float intensity;       // overall emissive multiplier
    float innerRadius;     // radius (0..1) where core is brightest
    float outerRadius;     // radius (0..1) where glow falls to zero
    float spikeCount;      // number of radial spikes
    float spikeSharpness;  // exponent for spike falloff
};

// Simple pseudo-noise using sin/cos — cheap and stable for shaders without a noise texture.
float turbulence(float2 uv, float t)
{
    float n = 0.0;
    float freq = 6.0;
    float amp = 1.0;
    // 3 octaves of sine-based turbulence
    n += (sin((uv.x + uv.y) * freq + t) * 0.5 + 0.5) * amp;
    freq *= 1.9; amp *= 0.6;
    n += (sin((uv.x * 1.7 - uv.y * 0.6) * freq + t * 1.5) * 0.5 + 0.5) * amp;
    freq *= 2.1; amp *= 0.5;
    n += (sin((uv.y * 2.9 + uv.x * 0.3) * freq + t * 2.3) * 0.5 + 0.5) * amp;
    return saturate(n / (1.0 + 0.6 + 0.3));
}

float4 PSMain(VSOutput input) : SV_Target
{
    // Sample base texture (assumed to be color in linear or sRGB depending on pipeline)
    float3 texRgb = DiffuseTex.Sample(DiffuseSamp, input.uv).rgb;

    // UV centered on the sun texture (0.5,0.5 = center)
    float2 uvc = input.uv - 0.5;
    float radial = length(uvc);
    float angle = atan2(uvc.y, uvc.x);

    // Surface detail / turbulence drives mottling and short-lived bright patches
    float surf = turbulence(input.uv * 3.0, time * 0.7);

    // Core mask: smooth falloff from innerRadius to outerRadius
    float coreMask = smoothstep(outerRadius, innerRadius, radial);

    // Star spikes: sharp radial streaks created by raising cos(angle * N) to a power
    float spikesRaw = abs(cos(angle * spikeCount));
    float spikes = pow(spikesRaw, spikeSharpness);
    // Attenuate spikes with radial mask so they only appear near the sun
    spikes *= smoothstep(outerRadius * 0.9, innerRadius * 0.3, radial);

    // Rim glow (broad corona) — soft band around the sun
    float corona = smoothstep(outerRadius * 1.2, outerRadius * 0.5, radial) * (0.8 + 0.4 * surf);

    // Combine base texture with surface turbulence (adds brightness variation)
    float3 surfaceColor = lerp(texRgb, texRgb * (1.0 + surf * 0.9), 0.65);

    // Emissive contribution: tinted by sunColor and modulated by masks
    float3 emissive = sunColor * intensity * (
        coreMask * (0.9 + 0.6 * surf)      // bright core with surface detail
        + corona * 0.45                    // broad rim
        + spikes * 1.6                     // sharp directional spikes
    );

    // Optional subtle lens artifact: a faint ghost near center using higher frequency of UV
    float ghost = pow(saturate(1.0 - radial * 1.8), 2.0) * (0.2 + 0.8 * turbulence(input.uv * 8.0 + float2(0.2, 0.7), time * 1.8));
    emissive += sunColor * intensity * ghost * 0.25;

    // Combine surface color and emissive, apply simple exposure (reinhard-like tone mapping)
    float3 hdr = surfaceColor + emissive;
    float3 mapped = 1.0 - exp(-hdr); // simple exposure curve

    // Final gamma correction to sRGB
    float3 finalColor = pow(saturate(mapped), 1.0 / 2.2);

    return float4(finalColor, 1.0);
}
