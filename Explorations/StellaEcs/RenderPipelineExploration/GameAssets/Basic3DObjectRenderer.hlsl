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
    float3 viewDir : TEXCOORD3; // world-space view direction (computed in VS to avoid fragment cbuffer use)
    float2 uv   : TEXCOORD1;
};

// Vertex uniform buffer for MVP matrix (unchanged)
cbuffer VSParams : register(b0, space1)
{
    float4x4 mvp;
    float4x4 model;
    float4x4 modelWorld; // world-space model matrix (no camera-relative subtraction)
    float3   cameraPosition;
};

struct Material
{
    float3 ambient;
    float pad0;
    float3 diffuse;
    float pad1;
    float3 specular;
    float shininess;
    float ambientStrength;
    float3 _pad_material;
};

// Light structures (matching C# components)
struct DirectionalLight
{
    float3 direction;
    float3 color;
    float intensity;
};

struct PointLight
{
    float3 position;
    float3 color;
    float intensity;
    float range;
};

struct SpotLight
{
    float3 position;
    float3 direction;
    float3 color;
    float intensity;
    float range;
    float innerAngle;
    float outerAngle;
};

// Light data buffers
cbuffer DirectionalLightsData : register(b0, space3)
{
    DirectionalLight directionalLights[4];
}

cbuffer PointLightsData : register(b1, space3)
{
    PointLight pointLights[60];
}

cbuffer SpotLightsData : register(b2, space3)
{
    SpotLight spotLights[16];
}

// Light counts
cbuffer LightCounts : register(b3, space3)
{
    uint directionalLightCount;
    uint pointLightCount;
    uint spotLightCount;
}

// Material properties
cbuffer MaterialProperties : register(b4, space3)
{
    Material material;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos  = mul(mvp, float4(input.pos, 1.0));
    // transform normal to world space using the world model matrix so lighting uses world-space normals
    o.nrm  = normalize(mul((float3x3)modelWorld, input.nrm));
    o.col  = input.col;
    o.mpos = input.pos; // model space
    // modelWorld is the world-space model matrix (no camera subtraction) used by the shadow pass
    o.wpos = mul(modelWorld, float4(input.pos, 1.0)).xyz; // world space for shadow sampling
    // compute world-space view direction in VS so the fragment shader doesn't need to read the vertex-only cbuffer
    o.viewDir = normalize(cameraPosition - o.wpos);
    o.uv   = input.uv;
    return o;
}

// Texture bindings (space2 for fragment shaders in SPIR-V)
Texture2D    DiffuseTex  : register(t0, space2);
SamplerState DiffuseSamp : register(s0, space2);

// Shadow map bindings
Texture2D    ShadowMap   : register(t1, space2);
SamplerState ShadowSamp  : register(s1, space2);

// Shadow map parameters
cbuffer ShadowParams : register(b5, space3)
{
    float4x4 lightViewProjection;
    float shadowBias;
    float shadowIntensity;
};

// Lighting calculation functions
// Use a sampled albedo map (passed in) for the diffuse term and keep material.specular/shininess for specular.
// Return diffuse and specular separately so the PS can combine them appropriately (Phong model)
void CalculateDirectionalLight(DirectionalLight light, float3 normal, float3 viewDir, float3 albedo, out float3 outDiffuse, out float3 outSpecular)
{
    // Directional light direction (from fragment towards light)
    float3 lightDir = normalize(-light.direction);

    // Diffuse (Lambert) driven by texture albedo (optionally tinted on the CPU by material.diffuse)
    float diff = max(dot(normal, lightDir), 0.0);
    outDiffuse = light.color * (diff * albedo) * light.intensity;

    // Specular (Phong) still uses material.specular/shininess
    float3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), max(1.0, material.shininess));
    outSpecular = light.color * (spec * material.specular) * light.intensity;
}

void CalculatePointLight(PointLight light, float3 normal, float3 fragPos, float3 viewDir, float3 albedo, out float3 outDiffuse, out float3 outSpecular)
{
    float3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);

    // Attenuation (common quadratic approximation)
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * distance * distance);
    // Optional smooth falloff near range (note: keep as-is if host code expects this behavior)
    attenuation *= smoothstep(light.range, light.range * 0.8, distance);

    // Diffuse driven by albedo
    float diff = max(dot(normal, lightDir), 0.0);
    outDiffuse = light.color * (diff * albedo) * light.intensity * attenuation;

    // Specular (Phong)
    float3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), max(1.0, material.shininess));
    outSpecular = light.color * (spec * material.specular) * light.intensity * attenuation;
}

void CalculateSpotLight(SpotLight light, float3 normal, float3 fragPos, float3 viewDir, float3 albedo, out float3 outDiffuse, out float3 outSpecular)
{
    float3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);

    // Attenuation
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * distance * distance);
    attenuation *= smoothstep(light.range, light.range * 0.8, distance);

    // Spotlight intensity (inner/outer cone)
    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = light.innerAngle - light.outerAngle;
    float coneIntensity = clamp((theta - light.outerAngle) / max(0.0001, epsilon), 0.0, 1.0);
    attenuation *= coneIntensity;

    // Diffuse driven by albedo
    float diff = max(dot(normal, lightDir), 0.0);
    outDiffuse = light.color * (diff * albedo) * light.intensity * attenuation;

    // Specular (Phong)
    float3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), max(1.0, material.shininess));
    outSpecular = light.color * (spec * material.specular) * light.intensity * attenuation;
}



float4 PSMain(VSOutput input) : SV_Target
{
    // Sample the diffuse texture (albedo)
    float3 texRgb = DiffuseTex.Sample(DiffuseSamp, input.uv).rgb;

    // Allow material.diffuse to act as an optional tint on the albedo.
    // If you want the texture to completely replace material.diffuse, set material.diffuse = float3(1,1,1) on the CPU.
    float3 albedoTinted = texRgb * material.diffuse;

    // If no lights, return albedo (tinted) modulated by ambient + full base albedo
    if (directionalLightCount == 0 && pointLightCount == 0 && spotLightCount == 0)
    {
        float3 ambientOnly = material.ambient * material.ambientStrength;
        return float4(albedoTinted * (ambientOnly + 1.0), 1.0);
    }

    // Normalize interpolants
    float3 normal = normalize(input.nrm);

    // View direction provided by VS as world-space vector
    float3 viewDir = normalize(input.viewDir);

    // Accumulators for Phong components
    float3 ambient = material.ambient * material.ambientStrength;
    float3 diffuseAccum = float3(0.0, 0.0, 0.0);
    float3 specularAccum = float3(0.0, 0.0, 0.0);

    // Directional lights
    for (uint i = 0; i < directionalLightCount; i++)
    {
        float3 d, s;
        CalculateDirectionalLight(directionalLights[i], normal, viewDir, albedoTinted, d, s);
        diffuseAccum += d;
        specularAccum += s;
    }

    // Point lights
    for (uint j = 0; j < pointLightCount; j++)
    {
        float3 d, s;
        CalculatePointLight(pointLights[j], normal, input.wpos, viewDir, albedoTinted, d, s);
        diffuseAccum += d;
        specularAccum += s;
    }

    // Spot lights
    for (uint k = 0; k < spotLightCount; k++)
    {
        float3 d, s;
        CalculateSpotLight(spotLights[k], normal, input.wpos, viewDir, albedoTinted, d, s);
        diffuseAccum += d;
        specularAccum += s;
    }

    // Final color: textured albedo drives diffuse; specular is added on top.
    float3 finalColor = albedoTinted * (ambient + diffuseAccum) + specularAccum;

    return float4(finalColor, 1.0);
}