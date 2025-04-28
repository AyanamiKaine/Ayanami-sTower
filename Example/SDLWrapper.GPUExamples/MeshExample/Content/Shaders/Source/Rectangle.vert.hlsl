// Input struct matching the C# Vertex struct and pipeline state
// Locations must match SDL_GPUVertexAttribute setup
struct VS_INPUT
{
    float2 Position  : TEXCOORD0; // Corresponds to location 0
    float4 Color     : TEXCOORD1; // Corresponds to location 1
    float2 TexCoord  : TEXCOORD2; // Corresponds to location 2 (unused but part of struct)
    // uint InstanceId : SV_InstanceID; // Optional: if using instancing
};

// Output struct (remains the same)
struct VS_OUTPUT
{
    float4 Color     : TEXCOORD0; // Pass color to Fragment Shader
    float4 Position  : SV_Position; // Clip space position
};

VS_OUTPUT main(VS_INPUT input)
{
    VS_OUTPUT output;

    // Directly use the input data from the vertex buffer
    output.Position = float4(input.Position.x, input.Position.y, 0.0f, 1.0f); // Convert float2 pos to float4
    output.Color = input.Color; // Pass the vertex color through

    return output;
}