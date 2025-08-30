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
    PointLight pointLights[120];
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
    float3 ambientColor;
    float3 diffuseColor;
    float3 specularColor;
    float shininess;
    float ambientStrength;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;
    o.pos  = mul(mvp, float4(input.pos, 1.0));
    o.nrm  = normalize(input.nrm);
    o.col  = input.col;
    o.mpos = input.pos; // model space
    o.wpos = mul(model, float4(input.pos, 1.0)).xyz; // world space
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
float3 CalculateDirectionalLight(DirectionalLight light, float3 normal, float3 viewDir)
{
    // Diffuse
    float diff = max(dot(normal, -light.direction), 0.0);
    float3 diffuse = light.color * diff * light.intensity;

    // Specular (Blinn-Phong)
    float3 halfwayDir = normalize(-light.direction + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);
    float3 specular = light.color * spec * light.intensity;

    return diffuse + specular;
}

float3 CalculatePointLight(PointLight light, float3 normal, float3 fragPos, float3 viewDir)
{
    float3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);

    // Attenuation
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * distance * distance);
    attenuation *= smoothstep(light.range, light.range * 0.8, distance); // Smooth falloff

    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    float3 diffuse = light.color * diff * light.intensity * attenuation;

    // Specular
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);
    float3 specular = light.color * spec * light.intensity * attenuation;

    return diffuse + specular;
}

float3 CalculateSpotLight(SpotLight light, float3 normal, float3 fragPos, float3 viewDir)
{
    float3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);

    // Attenuation
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * distance * distance);
    attenuation *= smoothstep(light.range, light.range * 0.8, distance);

    // Spotlight intensity
    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = light.innerAngle - light.outerAngle;
    float intensity = clamp((theta - light.outerAngle) / epsilon, 0.0, 1.0);
    attenuation *= intensity;

    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    float3 diffuse = light.color * diff * light.intensity * attenuation;

    // Specular
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);
    float3 specular = light.color * spec * light.intensity * attenuation;

    return diffuse + specular;
}

float CalculateShadow(float3 worldPos)
{
    // Transform world position to light's clip space
    float4 lightClipPos = mul(lightViewProjection, float4(worldPos, 1.0));
    lightClipPos.xyz /= lightClipPos.w; // Perspective divide
    
    // Convert to texture coordinates (0 to 1)
    float2 shadowUV = lightClipPos.xy * 0.5 + 0.5;
    
    // Check if position is within shadow map bounds
    if (shadowUV.x < 0.0 || shadowUV.x > 1.0 || shadowUV.y < 0.0 || shadowUV.y > 1.0)
    {
        return 1.0; // No shadow
    }
    
    // Sample shadow map with PCF (Percentage Closer Filtering)
    float shadow = 0.0;
    float2 texelSize = 1.0 / 2048.0; // Shadow map size
    
    // 3x3 PCF kernel
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            float2 offset = float2(x, y) * texelSize;
            float shadowDepth = ShadowMap.Sample(ShadowSamp, shadowUV + offset).r;
            shadow += (lightClipPos.z - shadowBias > shadowDepth) ? shadowIntensity : 1.0;
        }
    }
    
    shadow /= 9.0; // Average the 9 samples
    
    return shadow;
}

float4 PSMain(VSOutput input) : SV_Target
{
    // Sample the texture
    float3 texRgb = DiffuseTex.Sample(DiffuseSamp, input.uv).rgb;

    // If no lights, just return texture color with ambient
    if (directionalLightCount == 0 && pointLightCount == 0 && spotLightCount == 0)
    {
        float3 ambient = ambientColor * ambientStrength;
        return float4(texRgb * (ambient + diffuseColor), 1.0);
    }

    // Initialize lighting
    float3 lighting = float3(0.0, 0.0, 0.0);

    // View direction (simplified - assuming camera at origin for now)
    float3 viewDir = normalize(float3(0.0, 0.0, 1.0) - input.mpos);

    // Calculate directional lights
    for (uint i = 0; i < directionalLightCount; i++)
    {
        float3 lightContribution = CalculateDirectionalLight(directionalLights[i], input.nrm, viewDir);
        float shadow = CalculateShadow(input.wpos);
        lighting += lightContribution * shadow;
    }

    // Calculate point lights
    for (uint j = 0; j < pointLightCount; j++)
    {
        lighting += CalculatePointLight(pointLights[j], input.nrm, input.mpos, viewDir);
    }

    // Calculate spot lights
    for (uint k = 0; k < spotLightCount; k++)
    {
        lighting += CalculateSpotLight(spotLights[k], input.nrm, input.mpos, viewDir);
    }

    // Combine texture color with lighting
    float3 ambient = ambientColor * ambientStrength;
    float3 finalColor = texRgb * (ambient + diffuseColor + lighting);

    return float4(finalColor, 1.0);
}