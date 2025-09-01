using MoonWorks.Graphics;
using MoonWorks.Storage;
using System;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/*
Shader resource bindings must be authored to follow a particular order depending on the shader format.

For SPIR-V shaders, use the following resource sets:

0: Sampled textures, followed by read-only storage textures, followed by read-only storage buffers
1: Read-write storage textures, followed by read-write storage buffers
2: Uniform buffers

For DXBC and DXIL shaders, use the following register order:

(t[n], space0): Sampled textures, followed by read-only storage textures, followed by read-only storage buffers
(u[n], space1): Read-write storage textures, followed by read-write storage buffers
(b[n], space2): Uniform buffers
For MSL/metallib, use the following order:

[[buffer]]: Uniform buffers, followed by read-only storage buffers, followed by read-write storage buffers
[[texture]]: Sampled textures, followed by read-only storage textures, followed by read-write storage textures
*/

/// <summary>
/// Factory for creating compute pipelines with a fluent builder.
/// </summary>
public class ComputePipelineFactory
{
    private readonly GraphicsDevice _graphicsDevice;
    /// <summary>
    /// Initializes a new instance of the ComputePipelineFactory class.
    /// </summary>
    public ComputePipelineFactory(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Creates a builder for a compute pipeline.
    /// </summary>
    public ComputePipelineBuilder CreatePipeline(string name = "ComputePipeline")
    {
        return new ComputePipelineBuilder(_graphicsDevice, name);
    }
}

/// <summary>
/// Fluent builder for compute pipelines.
/// </summary>
public class ComputePipelineBuilder
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _name;

    private ShaderFormat _format = ShaderFormat.SPIRV;
    private uint _numSamplers = 0;
    private uint _numReadonlyStorageTextures = 0;
    private uint _numReadonlyStorageBuffers = 0;
    private uint _numReadWriteStorageTextures = 0;
    private uint _numReadWriteStorageBuffers = 0;
    private uint _numUniformBuffers = 0;
    private uint _threadCountX = 1;
    private uint _threadCountY = 1;
    private uint _threadCountZ = 1;
    private uint _props = 0;
    /// <summary>
    /// Initializes a new instance of the ComputePipelineBuilder class.
    /// </summary>
    public ComputePipelineBuilder(GraphicsDevice graphicsDevice, string name)
    {
        _graphicsDevice = graphicsDevice;
        _name = name;
    }

    /// <summary>
    /// Sets the format of the compute pipeline.
    /// </summary>
    public ComputePipelineBuilder WithFormat(ShaderFormat format) { _format = format; return this; }
    /// <summary>
    /// Sets the number of sampler bindings for the compute pipeline.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumSamplers(uint n) { _numSamplers = n; return this; }
    /// <summary>
    /// Sets the number of readonly storage textures for the compute pipeline.  
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumReadonlyStorageTextures(uint n) { _numReadonlyStorageTextures = n; return this; }
    /// <summary>
    /// Sets the number of readonly storage buffers for the compute pipeline.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumReadonlyStorageBuffers(uint n) { _numReadonlyStorageBuffers = n; return this; }
    /// <summary>
    /// Sets the number of read-write storage textures for the compute pipeline.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumReadWriteStorageTextures(uint n) { _numReadWriteStorageTextures = n; return this; }
    /// <summary>
    /// Sets the number of read-write storage buffers for the compute pipeline.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumReadWriteStorageBuffers(uint n) { _numReadWriteStorageBuffers = n; return this; }
    /// <summary>
    /// Sets the number of uniform buffers for the compute pipeline.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithNumUniformBuffers(uint n) { _numUniformBuffers = n; return this; }
    /// <summary>
    /// Sets the thread counts for the compute pipeline.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithThreadCounts(uint x, uint y = 1, uint z = 1) { _threadCountX = x; _threadCountY = y; _threadCountZ = z; return this; }
    /// <summary>
    /// Sets the properties for the compute pipeline.
    /// </summary>
    /// <param name="props"></param>
    /// <returns></returns>
    public ComputePipelineBuilder WithProps(uint props) { _props = props; return this; }

    /// <summary>
    /// Builds a compute pipeline using a precompiled bytecode file via TitleStorage.
    /// </summary>
    public ComputePipeline BuildFromStorage(TitleStorage storage, string filePath, string entryPoint = "main")
    {
        var createInfo = new ComputePipelineCreateInfo
        {
            Format = _format,
            NumSamplers = _numSamplers,
            NumReadonlyStorageTextures = _numReadonlyStorageTextures,
            NumReadonlyStorageBuffers = _numReadonlyStorageBuffers,
            NumReadWriteStorageTextures = _numReadWriteStorageTextures,
            NumReadWriteStorageBuffers = _numReadWriteStorageBuffers,
            NumUniformBuffers = _numUniformBuffers,
            ThreadCountX = _threadCountX,
            ThreadCountY = _threadCountY,
            ThreadCountZ = _threadCountZ,
            Name = _name,
            Props = _props
        };

        return ComputePipeline.Create(_graphicsDevice, storage, filePath, entryPoint, createInfo);
    }
}
