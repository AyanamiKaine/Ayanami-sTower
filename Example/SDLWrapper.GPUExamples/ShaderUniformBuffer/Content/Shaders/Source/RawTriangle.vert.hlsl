// Constant buffer containing data that changes per frame
// Explicitly use register space 1, which maps to Set 1 in SPIR-V for SDL_gpu
// We need to say space1 otherwise it wont work but why? 
// I was expecting it to be automatically use space 0.
// Am I setting the space wrong in the pipeline code?
cbuffer PerFrameConstants : register(b0, space1)
{
    uint frameCount; // Current frame number, updated by the CPU
};

// Input structure for the vertex shader
struct Input
{
    uint VertexIndex : SV_VertexID; // System-Value: The index of the vertex being processed
};

// Output structure for the vertex shader (passed to the pixel shader or rasterizer)
struct Output
{
    float4 Color : TEXCOORD0;      // Vertex color (interpolated for pixels)
    float4 Position : SV_Position; // Vertex position in clip space (System-Value)
};

// Main function for the vertex shader
Output main(Input input)
{
    Output output;
    float2 pos;
    float4 baseColor; // Use a temporary variable for the base color

    // Determine base position and fixed base color based on vertex index
    if (input.VertexIndex == 0)
    {
        pos = float2(-1.0f, -1.0f);          // Bottom-left
        baseColor = float4(1.0f, 0.0f, 0.0f, 1.0f); // Base Red
    }
    else if (input.VertexIndex == 1)
    {
        pos = float2(1.0f, -1.0f);           // Bottom-right
        baseColor = float4(0.0f, 1.0f, 0.0f, 1.0f); // Base Green
    }
    else // Assuming input.VertexIndex == 2
    {
        pos = float2(0.0f, 1.0f);            // Top-center
        baseColor = float4(0.0f, 0.0f, 1.0f, 1.0f); // Base Blue
    }

    // --- Using frameCount for COLOR (Handles High Frame Counts) ---

    // 1. Wrap frameCount using modulo to prevent floating point issues at large values.
    //    Choose a large enough number so the cycle isn't too short. 36000 = 10 minutes at 60fps.
    uint wrappedFrameCount = frameCount / 20;

    // 2. Convert the wrapped frameCount to a float and scale for animation speed.
    float time = (float)wrappedFrameCount * 0.05f; // Adjust 0.05f to make color change faster/slower

    // 3. Calculate oscillating color factors (range [0, 1]) using sin.
    float rFactor = (sin(time * 1.0f) * 0.5f + 0.5f);
    float gFactor = (sin(time * 1.3f + 2.0f) * 0.5f + 0.5f);
    float bFactor = (sin(time * 1.7f + 4.0f) * 0.5f + 0.5f);

    // 4. Apply the factors to the base color.
    output.Color.r = baseColor.r * rFactor;
    output.Color.g = baseColor.g * gFactor;
    output.Color.b = baseColor.b * bFactor;
    output.Color.a = baseColor.a; // Keep alpha the same

    // --- End of frameCount for color usage ---


    // Set the final clip-space position using the ORIGINAL, unmodified 'pos'
    output.Position = float4(pos, 0.0f, 1.0f);

    return output; // Return the processed vertex data
}