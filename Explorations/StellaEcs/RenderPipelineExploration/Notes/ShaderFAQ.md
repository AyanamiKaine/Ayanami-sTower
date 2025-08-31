https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader

## I am getting validation errors for my buffers that my shaders want to consume.

This is probably a simple bindings mistake.

Shader resource bindings must be authored to follow a particular order depending on the shader format.

For SPIR-V shaders (This will be our default target on all platforms!), use the following resource sets:

For vertex shaders:

0: Sampled textures, followed by storage textures, followed by storage buffers
1: Uniform buffers
For fragment shaders:

2: Sampled textures, followed by storage textures, followed by storage buffers
3: Uniform buffers
For DXBC and DXIL shaders, use the following register order:

For vertex shaders:

(t[n], space0): Sampled textures, followed by storage textures, followed by storage buffers
(s[n], space0): Samplers with indices corresponding to the sampled textures
(b[n], space1): Uniform buffers
For pixel shaders:

(t[n], space2): Sampled textures, followed by storage textures, followed by storage buffers
(s[n], space2): Samplers with indices corresponding to the sampled textures
(b[n], space3): Uniform buffers
For MSL/metallib, use the following order:

[[texture]]: Sampled textures, followed by storage textures
[[sampler]]: Samplers with indices corresponding to the sampled textures
[[buffer]]: Uniform buffers, followed by storage buffers. Vertex buffer 0 is bound at [[buffer(14)]], vertex buffer 1 at [[buffer(15)]], and so on. Rather than manually authoring vertex buffer indices, use the [[stage_in]] attribute which will automatically use the vertex input information from the SDL_GPUGraphicsPipeline.

## Why Do Constant Buffers Need to Be 16-Byte Aligned?

Why do we want to have a 16-byte aligned constant buffer? Because of performance mostly. And because the GPU mostly expects it. Its good practice to always 16-byte aligned it. I.e. 16 byte = (4 float);
