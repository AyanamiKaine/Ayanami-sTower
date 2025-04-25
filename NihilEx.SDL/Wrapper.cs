using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text; // Required for StringBuilder
using SDL3;
//THIS SHOULD BE REMOVED AT SOME POINT
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Use the namespace from your bindings file
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.SDLWrapper
{
    #region GPU Abstraction Enums

    // --- Mirror SDL_GPU Enums ---
    // (You already have these in your bindings, but redefining them
    // in the wrapper namespace can sometimes improve clarity and
    // decouple the wrapper slightly from the raw bindings enum names if desired.
    // Alternatively, just use the enums directly from SDL3.SDL namespace)

    /// <summary>
    /// Specifies GPU shader formats supported by the application.
    /// Maps to SDL_GPUShaderFormat.
    /// </summary>
    [Flags]
    public enum GpuShaderFormat : uint
    {
        Private = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_PRIVATE,
        SpirV = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
        Dxbc = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXBC,
        Dxil = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
        Msl = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
        MetalLib = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_METALLIB,
    }

    /// <summary>
    /// Specifies GPU buffer usage flags.
    /// Maps to SDL_GPUBufferUsageFlags.
    /// </summary>
    [Flags]
    public enum GpuBufferUsageFlags : uint
    {
        Vertex = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
        Index = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
        Indirect = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDIRECT,
        GraphicsStorageRead = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ,
        ComputeStorageRead = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ,
        ComputeStorageWrite = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE,

        // Convenience combinations
        StorageRead = GraphicsStorageRead | ComputeStorageRead,
        StorageWrite = ComputeStorageWrite, // Currently only ComputeStorageWrite exists
        StorageReadWrite = StorageRead | StorageWrite,
    }

    /// <summary>
    /// Specifies GPU texture usage flags.
    /// Maps to SDL_GPUTextureUsageFlags.
    /// </summary>
    [Flags]
    public enum GpuTextureUsageFlags : uint
    {
        Sampler = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
        ColorTarget = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
        DepthStencilTarget = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
        GraphicsStorageRead = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ,
        ComputeStorageRead = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ,
        ComputeStorageWrite = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE,

        // Convenience combinations
        StorageRead = GraphicsStorageRead | ComputeStorageRead,
        StorageWrite = ComputeStorageWrite,
        StorageReadWrite = StorageRead | StorageWrite,
    }

    // Add other GPU enums here if you want wrapper-specific versions,
    // otherwise use the SDL3.SDL enums directly (e.g., SDL_GPUTextureFormat, SDL_GPUCompareOp)

    #endregion

    #region GPU Abstraction Structs

    // --- Mirror SDL_GPU Structs ---
    // Create wrapper structs or use SDL3.SDL structs directly.
    // Using SDL3.SDL structs is simpler if they are well-defined.
    // Example: If you need to pass SDL_GPUTextureCreateInfo, you can use it directly.

    /// <summary>
    /// Wrapper for shader creation parameters.
    /// Consider creating such wrappers if you want to abstract away pointers
    /// or provide more managed constructors.
    /// </summary>
    public ref struct GpuShaderCreateInfo // Using ref struct for potentially large data
    {
        public ReadOnlySpan<byte> Code { get; set; }
        public string EntryPoint { get; set; } // Use string for convenience
        public GpuShaderFormat Format { get; set; } // Use wrapper enum
        public SDL_GPUShaderStage Stage { get; set; }
        public uint NumSamplers { get; set; }
        public uint NumStorageTextures { get; set; }
        public uint NumStorageBuffers { get; set; }
        public uint NumUniformBuffers { get; set; }

        // Add property for 'props' if needed

        // Helper to convert to the native struct for P/Invoke
        internal unsafe SDL_GPUShaderCreateInfo ToNative(byte* codePtr, byte* entryPointPtr)
        {
            return new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)Code.Length,
                code = codePtr,
                entrypoint = entryPointPtr,
                format = (SDL_GPUShaderFormat)Format,
                stage = Stage,
                num_samplers = NumSamplers,
                num_storage_textures = NumStorageTextures,
                num_storage_buffers = NumStorageBuffers,
                num_uniform_buffers = NumUniformBuffers,
                props =
                    0 // Set props if you add it
                ,
            };
        }
    }

    // Add other CreateInfo wrappers as needed (e.g., GpuTextureCreateInfo, GpuBufferCreateInfo)
    // Or, just use the SDL3.SDL.SDL_GPU*CreateInfo structs directly in the factory methods.

    #endregion

    #region GPU Resource Classes (IDisposable)

    /// <summary>
    /// Base class for disposable GPU resources tied to a GpuDevice.
    /// </summary>
    public abstract class GpuResource : IDisposable
    {
        protected GpuDevice _device;
        protected IntPtr _handle;
        protected bool _disposed = false;

        public IntPtr Handle =>
            _disposed ? throw new ObjectDisposedException(GetType().Name) : _handle;
        public GpuDevice Device =>
            _disposed ? throw new ObjectDisposedException(GetType().Name) : _device;
        public bool IsDisposed => _disposed;

        internal GpuResource(GpuDevice device, IntPtr handle)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _handle = handle;
            if (handle == IntPtr.Zero)
            {
                // This indicates an error during creation before the wrapper was constructed.
                // The factory method should have thrown already, but double-check.
                throw new ArgumentException("GPU resource handle cannot be zero.", nameof(handle));
            }
        }

        protected abstract void ReleaseNativeResource(GpuDevice device, IntPtr handle);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_handle != IntPtr.Zero && _device != null && !_device.IsDisposed)
                {
                    try
                    {
                        ReleaseNativeResource(_device, _handle);
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exceptions during resource release if necessary
                        Console.Error.WriteLine(
                            $"Error releasing {GetType().Name} ({_handle}): {ex.Message}"
                        );
                        // Potentially log using SDL_Log if safe
                    }
                }
                _handle = IntPtr.Zero; // Prevent further use
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~GpuResource()
        {
            Dispose(disposing: false);
        }
    }

    public sealed class GpuShader : GpuResource
    {
        // Store info needed later if required
        public GpuShaderFormat Format { get; }
        public SDL_GPUShaderStage Stage { get; }

        internal GpuShader(
            GpuDevice device,
            IntPtr handle,
            GpuShaderFormat format,
            SDL_GPUShaderStage stage
        )
            : base(device, handle)
        {
            Format = format;
            Stage = stage;
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUShader(device.Handle, handle);
        }
    }

    public sealed class GpuBuffer : GpuResource
    {
        public uint Size { get; }
        public GpuBufferUsageFlags Usage { get; }

        internal GpuBuffer(GpuDevice device, IntPtr handle, uint size, GpuBufferUsageFlags usage)
            : base(device, handle)
        {
            Size = size;
            Usage = usage;
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUBuffer(device.Handle, handle);
        }

        /// <summary>
        /// Sets a debug name for this buffer.
        /// </summary>
        /// <param name="name">The debug name.</param>
        public void SetName(string name)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SDL_SetGPUBufferName(Device.Handle, Handle, name);
        }
    }

    public sealed class GpuTexture : GpuResource
    {
        public uint Width { get; }
        public uint Height { get; }
        public uint LayerCountOrDepth { get; }
        public uint LevelCount { get; }
        public SDL_GPUTextureFormat Format { get; }
        public SDL_GPUTextureType Type { get; }
        public GpuTextureUsageFlags Usage { get; }
        public SDL_GPUSampleCount SampleCount { get; }

        internal GpuTexture(GpuDevice device, IntPtr handle, SDL_GPUTextureCreateInfo createInfo)
            : base(device, handle)
        {
            Width = createInfo.width;
            Height = createInfo.height;
            LayerCountOrDepth = createInfo.layer_count_or_depth;
            LevelCount = createInfo.num_levels;
            Format = createInfo.format;
            Type = createInfo.type;
            Usage = (GpuTextureUsageFlags)createInfo.usage; // Cast from SDL enum
            SampleCount = createInfo.sample_count;
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUTexture(device.Handle, handle);
        }

        /// <summary>
        /// Sets a debug name for this texture.
        /// </summary>
        /// <param name="name">The debug name.</param>
        public void SetName(string name)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SDL_SetGPUTextureName(Device.Handle, Handle, name);
        }
    }

    public sealed class GpuSampler : GpuResource
    {
        // Store sampler state if needed for reference
        internal GpuSampler(GpuDevice device, IntPtr handle, SDL_GPUSamplerCreateInfo createInfo)
            : base(device, handle)
        {
            // Store relevant createInfo fields if needed
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUSampler(device.Handle, handle);
        }
    }

    public sealed class GpuTransferBuffer : GpuResource
    {
        public uint Size { get; }
        public SDL_GPUTransferBufferUsage Usage { get; }

        internal GpuTransferBuffer(
            GpuDevice device,
            IntPtr handle,
            uint size,
            SDL_GPUTransferBufferUsage usage
        )
            : base(device, handle)
        {
            Size = size;
            Usage = usage;
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUTransferBuffer(device.Handle, handle);
        }

        /// <summary>
        /// Maps the transfer buffer for CPU access.
        /// </summary>
        /// <param name="cycle">
        /// If true, advances the internal buffer index to avoid overwriting data
        /// currently in use by the GPU. If false, maps the current buffer instance.
        /// </param>
        /// <returns>A pointer to the mapped memory.</returns>
        /// <exception cref="SDLException">Thrown if mapping fails.</exception>
        /// <remarks>Remember to call Unmap() when finished.</remarks>
        public unsafe void* Map(bool cycle = true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IntPtr ptr = SDL_MapGPUTransferBuffer(Device.Handle, Handle, cycle);
            SdlHost.ThrowOnNull(ptr, "Failed to map GPU transfer buffer");
            return (void*)ptr;
        }

        /// <summary>
        /// Unmaps a previously mapped transfer buffer.
        /// </summary>
        public void Unmap()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // SDL_UnmapGPUTransferBuffer returns void, assumes success if handle is valid.
            SDL_UnmapGPUTransferBuffer(Device.Handle, Handle);
        }
    }

    public sealed class GpuGraphicsPipeline : GpuResource
    {
        // Store pipeline state if needed
        internal GpuGraphicsPipeline(
            GpuDevice device,
            IntPtr handle,
            SDL_GPUGraphicsPipelineCreateInfo createInfo
        )
            : base(device, handle)
        {
            // Store relevant createInfo fields if needed
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUGraphicsPipeline(device.Handle, handle);
        }
    }

    public sealed class GpuComputePipeline : GpuResource
    {
        // Store pipeline state if needed
        internal GpuComputePipeline(
            GpuDevice device,
            IntPtr handle,
            SDL_GPUComputePipelineCreateInfo createInfo
        )
            : base(device, handle)
        {
            // Store relevant createInfo fields if needed
        }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUComputePipeline(device.Handle, handle);
        }
    }

    public sealed class GpuFence : GpuResource
    {
        internal GpuFence(GpuDevice device, IntPtr handle)
            : base(device, handle) { }

        protected override void ReleaseNativeResource(GpuDevice device, IntPtr handle)
        {
            SDL_ReleaseGPUFence(device.Handle, handle);
        }

        /// <summary>
        /// Queries the status of the fence.
        /// </summary>
        /// <returns>True if the commands associated with the fence have completed, false otherwise.</returns>
        public bool Query()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SDL_QueryGPUFence(Device.Handle, Handle);
        }
    }

    #endregion

    #region GPU Pass Scopes (IDisposable)

    /// <summary>
    /// Represents an active GPU render pass scope within a command buffer.
    /// Dispose this object to end the pass. Use with a 'using' statement.
    /// </summary>
    public readonly struct GpuRenderPass : IDisposable
    {
        private readonly GpuCommandBuffer _commandBuffer;

        // The pass handle is typically the same as the command buffer handle in SDL3 GPU API
        private readonly IntPtr _passHandle;

        internal GpuRenderPass(GpuCommandBuffer commandBuffer, IntPtr passHandle)
        {
            _commandBuffer = commandBuffer;
            _passHandle = passHandle;
        }

        /// <summary>
        /// Ends the render pass. Automatically called when disposing the object via 'using'.
        /// </summary>
        public void Dispose()
        {
            if (
                _commandBuffer != null
                && !_commandBuffer.IsSubmittedOrCancelled
                && _passHandle != IntPtr.Zero
            )
            {
                SDL_EndGPURenderPass(_passHandle);
            }
        }

        // --- Render Pass Commands ---

        public void BindGraphicsPipeline(GpuGraphicsPipeline pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            ObjectDisposedException.ThrowIf(pipeline.IsDisposed, pipeline);
            SDL_BindGPUGraphicsPipeline(_passHandle, pipeline.Handle);
        }

        public void SetViewport(SDL_GPUViewport viewport) // Pass SDL struct directly
        {
            SDL_SetGPUViewport(_passHandle, in viewport);
        }

        public void SetViewport(
            float x,
            float y,
            float w,
            float h,
            float minDepth = 0.0f,
            float maxDepth = 1.0f
        )
        {
            var viewport = new SDL_GPUViewport
            {
                x = x,
                y = y,
                w = w,
                h = h,
                min_depth = minDepth,
                max_depth = maxDepth,
            };
            SDL_SetGPUViewport(_passHandle, in viewport);
        }

        public void SetScissor(Rect rect) // Use wrapper Rect
        {
            SDL_Rect sdlRect = rect; // Implicit conversion
            SDL_SetGPUScissor(_passHandle, in sdlRect);
        }

        public void BindVertexBuffers(uint firstSlot, Span<SDL_GPUBufferBinding> bindings)
        {
            if (bindings.IsEmpty)
                return;
            // Check if bindings contain valid GpuBuffer handles? Might be too slow.
            SDL_BindGPUVertexBuffers(_passHandle, firstSlot, bindings, (uint)bindings.Length);
        }

        // Overload for convenience with GpuBuffer objects
        public void BindVertexBuffer(uint slot, GpuBuffer buffer, uint offset = 0)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
            var binding = new SDL_GPUBufferBinding { buffer = buffer.Handle, offset = offset };
            // Need Span<SDL_GPUBufferBinding> - allocate on stack if possible
            Span<SDL_GPUBufferBinding> bindings = stackalloc SDL_GPUBufferBinding[1];
            bindings[0] = binding;
            SDL_BindGPUVertexBuffers(_passHandle, slot, bindings, 1);
        }

        public void BindIndexBuffer(
            GpuBuffer buffer,
            uint offset,
            SDL_GPUIndexElementSize indexElementSize
        )
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
            var binding = new SDL_GPUBufferBinding { buffer = buffer.Handle, offset = offset };
            SDL_BindGPUIndexBuffer(_passHandle, in binding, indexElementSize);
        }

        public void BindFragmentSamplers(
            uint firstSlot,
            Span<SDL_GPUTextureSamplerBinding> bindings
        )
        {
            if (bindings.IsEmpty)
                return;
            SDL_BindGPUFragmentSamplers(_passHandle, firstSlot, bindings, (uint)bindings.Length);
        }

        // Convenience overload for single texture/sampler
        public void BindFragmentSampler(uint slot, GpuTexture texture, GpuSampler sampler)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ArgumentNullException.ThrowIfNull(sampler);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            ObjectDisposedException.ThrowIf(sampler.IsDisposed, sampler);

            var binding = new SDL_GPUTextureSamplerBinding
            {
                texture = texture.Handle,
                sampler = sampler.Handle,
            };
            Span<SDL_GPUTextureSamplerBinding> bindings =
                stackalloc SDL_GPUTextureSamplerBinding[1];
            bindings[0] = binding;
            SDL_BindGPUFragmentSamplers(_passHandle, slot, bindings, 1);
        }

        public void BindFragmentStorageTexture(uint slot, GpuTexture texture)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            Span<IntPtr> textures = stackalloc IntPtr[1];
            textures[0] = texture.Handle;
            SDL_BindGPUFragmentStorageTextures(_passHandle, slot, textures, 1);
        }

        public void BindFragmentStorageBuffer(uint slot, GpuBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
            Span<IntPtr> buffers = stackalloc IntPtr[1];
            buffers[0] = buffer.Handle;
            SDL_BindGPUFragmentStorageBuffers(_passHandle, slot, buffers, 1);
        }

        // Add BindVertexSamplers, BindVertexStorageTextures, BindVertexStorageBuffers similarly


        public void DrawPrimitives(
            uint vertexCount,
            uint instanceCount,
            uint firstVertex,
            uint firstInstance
        )
        {
            SDL_DrawGPUPrimitives(
                _passHandle,
                vertexCount,
                instanceCount,
                firstVertex,
                firstInstance
            );
        }

        public void DrawIndexedPrimitives(
            uint indexCount,
            uint instanceCount,
            uint firstIndex,
            int vertexOffset,
            uint firstInstance
        )
        {
            SDL_DrawGPUIndexedPrimitives(
                _passHandle,
                indexCount,
                instanceCount,
                firstIndex,
                vertexOffset,
                firstInstance
            );
        }

        // Add Indirect draw calls if needed


        public void SetBlendConstants(FColor color)
        {
            SDL_FColor sdlColor = color; // Implicit conversion
            SDL_SetGPUBlendConstants(_passHandle, sdlColor);
        }

        public void SetStencilReference(byte reference)
        {
            SDL_SetGPUStencilReference(_passHandle, reference);
        }
    }

    /// <summary>
    /// Represents an active GPU compute pass scope within a command buffer.
    /// Dispose this object to end the pass. Use with a 'using' statement.
    /// </summary>
    public readonly struct GpuComputePass : IDisposable
    {
        private readonly GpuCommandBuffer _commandBuffer;
        private readonly IntPtr _passHandle;

        internal GpuComputePass(GpuCommandBuffer commandBuffer, IntPtr passHandle)
        {
            _commandBuffer = commandBuffer;
            _passHandle = passHandle;
        }

        public void Dispose()
        {
            if (
                _commandBuffer != null
                && !_commandBuffer.IsSubmittedOrCancelled
                && _passHandle != IntPtr.Zero
            )
            {
                SDL_EndGPUComputePass(_passHandle);
            }
        }

        // --- Compute Pass Commands ---

        public void BindComputePipeline(GpuComputePipeline pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            ObjectDisposedException.ThrowIf(pipeline.IsDisposed, pipeline);
            SDL_BindGPUComputePipeline(_passHandle, pipeline.Handle);
        }

        public void BindStorageTextures(uint firstSlot, Span<IntPtr> textureHandles)
        {
            if (textureHandles.IsEmpty)
                return;
            SDL_BindGPUComputeStorageTextures(
                _passHandle,
                firstSlot,
                textureHandles,
                (uint)textureHandles.Length
            );
        }

        public void BindStorageTexture(uint slot, GpuTexture texture)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            Span<IntPtr> textures = stackalloc IntPtr[1];
            textures[0] = texture.Handle;
            SDL_BindGPUComputeStorageTextures(_passHandle, slot, textures, 1);
        }

        public void BindStorageBuffers(uint firstSlot, Span<IntPtr> bufferHandles)
        {
            if (bufferHandles.IsEmpty)
                return;
            SDL_BindGPUComputeStorageBuffers(
                _passHandle,
                firstSlot,
                bufferHandles,
                (uint)bufferHandles.Length
            );
        }

        public void BindStorageBuffer(uint slot, GpuBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
            Span<IntPtr> buffers = stackalloc IntPtr[1];
            buffers[0] = buffer.Handle;
            SDL_BindGPUComputeStorageBuffers(_passHandle, slot, buffers, 1);
        }

        public void BindSamplers(uint firstSlot, Span<SDL_GPUTextureSamplerBinding> bindings)
        {
            if (bindings.IsEmpty)
                return;
            SDL_BindGPUComputeSamplers(_passHandle, firstSlot, bindings, (uint)bindings.Length);
        }

        public void BindSampler(uint slot, GpuTexture texture, GpuSampler sampler)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ArgumentNullException.ThrowIfNull(sampler);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            ObjectDisposedException.ThrowIf(sampler.IsDisposed, sampler);

            var binding = new SDL_GPUTextureSamplerBinding
            {
                texture = texture.Handle,
                sampler = sampler.Handle,
            };
            Span<SDL_GPUTextureSamplerBinding> bindings =
                stackalloc SDL_GPUTextureSamplerBinding[1];
            bindings[0] = binding;
            SDL_BindGPUComputeSamplers(_passHandle, slot, bindings, 1);
        }

        public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            SDL_DispatchGPUCompute(_passHandle, groupCountX, groupCountY, groupCountZ);
        }

        public void DispatchIndirect(GpuBuffer buffer, uint offset)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
            SDL_DispatchGPUComputeIndirect(_passHandle, buffer.Handle, offset);
        }
    }

    /// <summary>
    /// Represents an active GPU copy pass scope within a command buffer.
    /// Dispose this object to end the pass. Use with a 'using' statement.
    /// </summary>
    public readonly struct GpuCopyPass : IDisposable
    {
        private readonly GpuCommandBuffer _commandBuffer;
        private readonly IntPtr _passHandle;

        internal GpuCopyPass(GpuCommandBuffer commandBuffer, IntPtr passHandle)
        {
            _commandBuffer = commandBuffer;
            _passHandle = passHandle;
        }

        public void Dispose()
        {
            if (
                _commandBuffer != null
                && !_commandBuffer.IsSubmittedOrCancelled
                && _passHandle != IntPtr.Zero
            )
            {
                SDL_EndGPUCopyPass(_passHandle);
            }
        }

        // --- Copy Pass Commands ---

        public void UploadToTexture(
            SDL_GPUTextureTransferInfo source,
            SDL_GPUTextureRegion destination,
            bool cycle = true
        )
        {
            // Add null checks for source.transfer_buffer and destination.texture if needed
            SDL_UploadToGPUTexture(_passHandle, in source, in destination, cycle);
        }

        // Convenience overload taking GpuTransferBuffer
        public void UploadToTexture(
            GpuTransferBuffer sourceBuffer,
            uint sourceOffset,
            uint pixelsPerRow,
            uint rowsPerLayer,
            GpuTexture destinationTexture,
            uint destMipLevel = 0,
            uint destLayer = 0,
            uint destX = 0,
            uint destY = 0,
            uint destZ = 0,
            uint destW = 0,
            uint destH = 0,
            uint destD = 0, // 0 implies full dimension
            bool cycle = true
        )
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationTexture);
            ObjectDisposedException.ThrowIf(sourceBuffer.IsDisposed, sourceBuffer);
            ObjectDisposedException.ThrowIf(destinationTexture.IsDisposed, destinationTexture);

            var source = new SDL_GPUTextureTransferInfo
            {
                transfer_buffer = sourceBuffer.Handle,
                offset = sourceOffset,
                pixels_per_row = pixelsPerRow,
                rows_per_layer = rowsPerLayer,
            };
            var destination = new SDL_GPUTextureRegion
            {
                texture = destinationTexture.Handle,
                mip_level = destMipLevel,
                layer = destLayer,
                x = destX,
                y = destY,
                z = destZ,
                w = destW == 0 ? destinationTexture.Width : destW,
                h = destH == 0 ? destinationTexture.Height : destH,
                d =
                    destD == 0
                        ? destinationTexture.LayerCountOrDepth
                        : destD // Depth for 3D, Layers for array/cube
                ,
            };
            SDL_UploadToGPUTexture(_passHandle, in source, in destination, cycle);
        }

        public void UploadToBuffer(
            SDL_GPUTransferBufferLocation source,
            SDL_GPUBufferRegion destination,
            bool cycle = true
        )
        {
            SDL_UploadToGPUBuffer(_passHandle, in source, in destination, cycle);
        }

        // Convenience overload
        public void UploadToBuffer(
            GpuTransferBuffer sourceBuffer,
            uint sourceOffset,
            GpuBuffer destinationBuffer,
            uint destOffset,
            uint size,
            bool cycle = true
        )
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationBuffer);
            ObjectDisposedException.ThrowIf(sourceBuffer.IsDisposed, sourceBuffer);
            ObjectDisposedException.ThrowIf(destinationBuffer.IsDisposed, destinationBuffer);

            var source = new SDL_GPUTransferBufferLocation
            {
                transfer_buffer = sourceBuffer.Handle,
                offset = sourceOffset,
            };
            var destination = new SDL_GPUBufferRegion
            {
                buffer = destinationBuffer.Handle,
                offset = destOffset,
                size = size,
            };
            SDL_UploadToGPUBuffer(_passHandle, in source, in destination, cycle);
        }

        public void DownloadFromTexture(
            SDL_GPUTextureRegion source,
            SDL_GPUTextureTransferInfo destination
        )
        {
            SDL_DownloadFromGPUTexture(_passHandle, in source, in destination);
        }

        // Convenience overload
        public void DownloadFromTexture(
            GpuTexture sourceTexture,
            GpuTransferBuffer destinationBuffer,
            uint destOffset,
            out uint pixelsPerRow,
            out uint rowsPerLayer, // Output layout info
            uint srcMipLevel = 0,
            uint srcLayer = 0,
            uint srcX = 0,
            uint srcY = 0,
            uint srcZ = 0,
            uint srcW = 0,
            uint srcH = 0,
            uint srcD = 0 // 0 implies full dimension
        )
        {
            ArgumentNullException.ThrowIfNull(sourceTexture);
            ArgumentNullException.ThrowIfNull(destinationBuffer);
            ObjectDisposedException.ThrowIf(sourceTexture.IsDisposed, sourceTexture);
            ObjectDisposedException.ThrowIf(destinationBuffer.IsDisposed, destinationBuffer);

            var source = new SDL_GPUTextureRegion
            {
                texture = sourceTexture.Handle,
                mip_level = srcMipLevel,
                layer = srcLayer,
                x = srcX,
                y = srcY,
                z = srcZ,
                w = srcW == 0 ? sourceTexture.Width : srcW,
                h = srcH == 0 ? sourceTexture.Height : srcH,
                d = srcD == 0 ? sourceTexture.LayerCountOrDepth : srcD,
            };
            // Calculate default layout based on texture format and dimensions
            // This might need adjustment based on specific format block sizes
            pixelsPerRow = source.w;
            rowsPerLayer = source.h; // Only relevant for 3D/Array textures

            var destination = new SDL_GPUTextureTransferInfo
            {
                transfer_buffer = destinationBuffer.Handle,
                offset = destOffset,
                pixels_per_row = pixelsPerRow,
                rows_per_layer = rowsPerLayer,
            };
            SDL_DownloadFromGPUTexture(_passHandle, in source, in destination);
        }

        public void DownloadFromBuffer(
            SDL_GPUBufferRegion source,
            SDL_GPUTransferBufferLocation destination
        )
        {
            SDL_DownloadFromGPUBuffer(_passHandle, in source, in destination);
        }

        // Convenience overload
        public void DownloadFromBuffer(
            GpuBuffer sourceBuffer,
            uint sourceOffset,
            uint size,
            GpuTransferBuffer destinationBuffer,
            uint destOffset
        )
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationBuffer);
            ObjectDisposedException.ThrowIf(sourceBuffer.IsDisposed, sourceBuffer);
            ObjectDisposedException.ThrowIf(destinationBuffer.IsDisposed, destinationBuffer);

            var source = new SDL_GPUBufferRegion
            {
                buffer = sourceBuffer.Handle,
                offset = sourceOffset,
                size = size,
            };
            var destination = new SDL_GPUTransferBufferLocation
            {
                transfer_buffer = destinationBuffer.Handle,
                offset = destOffset,
            };
            SDL_DownloadFromGPUBuffer(_passHandle, in source, in destination);
        }

        public void CopyBufferToBuffer(
            SDL_GPUBufferLocation source,
            SDL_GPUBufferLocation destination,
            uint size,
            bool cycle = false
        ) // Cycling less common for buffer-to-buffer?
        {
            SDL_CopyGPUBufferToBuffer(_passHandle, in source, in destination, size, cycle);
        }

        // Convenience overload
        public void CopyBufferToBuffer(
            GpuBuffer sourceBuffer,
            uint sourceOffset,
            GpuBuffer destinationBuffer,
            uint destOffset,
            uint size,
            bool cycle = false
        )
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationBuffer);
            ObjectDisposedException.ThrowIf(sourceBuffer.IsDisposed, sourceBuffer);
            ObjectDisposedException.ThrowIf(destinationBuffer.IsDisposed, destinationBuffer);

            var source = new SDL_GPUBufferLocation
            {
                buffer = sourceBuffer.Handle,
                offset = sourceOffset,
            };
            var destination = new SDL_GPUBufferLocation
            {
                buffer = destinationBuffer.Handle,
                offset = destOffset,
            };
            SDL_CopyGPUBufferToBuffer(_passHandle, in source, in destination, size, cycle);
        }

        public void CopyTextureToTexture(
            SDL_GPUTextureLocation source,
            SDL_GPUTextureLocation destination,
            uint w,
            uint h,
            uint d,
            bool cycle = false
        )
        {
            SDL_CopyGPUTextureToTexture(_passHandle, in source, in destination, w, h, d, cycle);
        }

        // Convenience overload
        public void CopyTextureToTexture(
            GpuTexture sourceTexture,
            uint srcMipLevel,
            uint srcLayer,
            uint srcX,
            uint srcY,
            uint srcZ,
            GpuTexture destinationTexture,
            uint dstMipLevel,
            uint dstLayer,
            uint dstX,
            uint dstY,
            uint dstZ,
            uint width,
            uint height,
            uint depth,
            bool cycle = false
        )
        {
            ArgumentNullException.ThrowIfNull(sourceTexture);
            ArgumentNullException.ThrowIfNull(destinationTexture);
            ObjectDisposedException.ThrowIf(sourceTexture.IsDisposed, sourceTexture);
            ObjectDisposedException.ThrowIf(destinationTexture.IsDisposed, destinationTexture);

            var source = new SDL_GPUTextureLocation
            {
                texture = sourceTexture.Handle,
                mip_level = srcMipLevel,
                layer = srcLayer,
                x = srcX,
                y = srcY,
                z = srcZ,
            };
            var destination = new SDL_GPUTextureLocation
            {
                texture = destinationTexture.Handle,
                mip_level = dstMipLevel,
                layer = dstLayer,
                x = dstX,
                y = dstY,
                z = dstZ,
            };
            SDL_CopyGPUTextureToTexture(
                _passHandle,
                in source,
                in destination,
                width,
                height,
                depth,
                cycle
            );
        }

        public void GenerateMipmaps(GpuTexture texture)
        {
            ArgumentNullException.ThrowIfNull(texture);
            ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
            SDL_GenerateMipmapsForGPUTexture(_passHandle, texture.Handle);
        }

        public void BlitTexture(SDL_GPUBlitInfo info)
        {
            SDL_BlitGPUTexture(_passHandle, info);
        }

        // Convenience overload for simple blit
        public void BlitTexture(
            GpuTexture sourceTexture,
            GpuTexture destinationTexture,
            SDL_GPUFilter filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            SDL_FlipMode flipMode = SDL_FlipMode.SDL_FLIP_NONE,
            bool cycle = false
        )
        {
            ArgumentNullException.ThrowIfNull(sourceTexture);
            ArgumentNullException.ThrowIfNull(destinationTexture);
            ObjectDisposedException.ThrowIf(sourceTexture.IsDisposed, sourceTexture);
            ObjectDisposedException.ThrowIf(destinationTexture.IsDisposed, destinationTexture);

            var srcRegion = new SDL_GPUBlitRegion
            {
                texture = sourceTexture.Handle,
                mip_level = 0,
                layer_or_depth_plane = 0,
                x = 0,
                y = 0,
                w = sourceTexture.Width,
                h = sourceTexture.Height,
            };
            var dstRegion = new SDL_GPUBlitRegion
            {
                texture = destinationTexture.Handle,
                mip_level = 0,
                layer_or_depth_plane = 0,
                x = 0,
                y = 0,
                w = destinationTexture.Width,
                h = destinationTexture.Height,
            };

            var info = new SDL_GPUBlitInfo
            {
                source = srcRegion,
                destination = dstRegion,
                filter = filter,
                flip_mode = flipMode,
                cycle = cycle,
                load_op =
                    SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE // Usually don't clear for blit
                ,
            };
            SDL_BlitGPUTexture(_passHandle, in info);
        }
    }

    #endregion

    #region GPU Command Buffer

    /// <summary>
    /// Represents a command buffer for recording GPU operations.
    /// Acquire from a GpuDevice, record commands using pass scopes, and then Submit or Cancel.
    /// This class itself is NOT disposable; its lifetime is managed by acquire/submit/cancel.
    /// </summary>
    public sealed class GpuCommandBuffer
    {
        private readonly GpuDevice _device;
        private readonly IntPtr _handle;
        private bool _isSubmittedOrCancelled = false;

        public IntPtr Handle =>
            _isSubmittedOrCancelled
                ? throw new InvalidOperationException(
                    "Command buffer has already been submitted or cancelled."
                )
                : _handle;
        public GpuDevice Device => _device; // No disposed check needed here as Device outlives CommandBuffer typically
        public bool IsSubmittedOrCancelled => _isSubmittedOrCancelled;

        internal GpuCommandBuffer(GpuDevice device, IntPtr handle)
        {
            _device = device;
            _handle = handle;
        }

        // --- Pass Creation ---

        /// <summary>
        /// Begins a render pass within this command buffer.
        /// </summary>
        /// <param name="colorTargetInfos">Information about the color render targets.</param>
        /// <param name="depthStencilTargetInfo">Optional information about the depth/stencil target.</param>
        /// <returns>A GpuRenderPass object representing the active pass scope. Dispose it to end the pass.</returns>
        /// <remarks>Use a 'using' statement for automatic disposal and ending of the pass.</remarks>
        public GpuRenderPass BeginRenderPass(
            Span<SDL_GPUColorTargetInfo> colorTargetInfos,
            SDL_GPUDepthStencilTargetInfo depthStencilTargetInfo = default
        )
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this); // Use custom message?
            // SDL_BeginGPURenderPass returns the command buffer handle itself to be used as the pass handle
            IntPtr passHandle = SDL_BeginGPURenderPass(
                Handle,
                colorTargetInfos,
                (uint)colorTargetInfos.Length,
                depthStencilTargetInfo.texture == IntPtr.Zero
                    ? ref Unsafe.NullRef<SDL_GPUDepthStencilTargetInfo>()
                    : ref depthStencilTargetInfo
            // Pass default if texture is null,
            );
            SdlHost.ThrowOnNull(passHandle, "Failed to begin GPU render pass");
            return new GpuRenderPass(this, passHandle);
        }

        // Convenience overload for single color target, no depth/stencil
        public GpuRenderPass BeginRenderPass(
            GpuTexture colorTarget,
            SDL_GPULoadOp loadOp = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            SDL_GPUStoreOp storeOp = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            FColor clearColor = default,
            bool cycle = true
        )
        {
            ArgumentNullException.ThrowIfNull(colorTarget);
            ObjectDisposedException.ThrowIf(colorTarget.IsDisposed, colorTarget);

            var info = new SDL_GPUColorTargetInfo
            {
                texture = colorTarget.Handle,
                mip_level = 0,
                layer_or_depth_plane = 0,
                clear_color = clearColor,
                load_op = loadOp,
                store_op = storeOp,
                cycle = cycle,
            };
            Span<SDL_GPUColorTargetInfo> infos = stackalloc SDL_GPUColorTargetInfo[1];
            infos[0] = info;
            return BeginRenderPass(infos);
        }

        /// <summary>
        /// Begins a compute pass within this command buffer.
        /// </summary>
        /// <param name="storageTextureBindings">Optional read/write storage texture bindings.</param>
        /// <param name="storageBufferBindings">Optional read/write storage buffer bindings.</param>
        /// <returns>A GpuComputePass object representing the active pass scope. Dispose it to end the pass.</returns>
        public GpuComputePass BeginComputePass(
            Span<SDL_GPUStorageTextureReadWriteBinding> storageTextureBindings = default,
            Span<SDL_GPUStorageBufferReadWriteBinding> storageBufferBindings = default
        )
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            IntPtr passHandle = SDL_BeginGPUComputePass(
                Handle,
                storageTextureBindings,
                (uint)storageTextureBindings.Length,
                storageBufferBindings,
                (uint)storageBufferBindings.Length
            );
            SdlHost.ThrowOnNull(passHandle, "Failed to begin GPU compute pass");
            return new GpuComputePass(this, passHandle);
        }

        /// <summary>
        /// Begins a copy pass within this command buffer.
        /// </summary>
        /// <returns>A GpuCopyPass object representing the active pass scope. Dispose it to end the pass.</returns>
        public GpuCopyPass BeginCopyPass()
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            IntPtr passHandle = SDL_BeginGPUCopyPass(Handle);
            SdlHost.ThrowOnNull(passHandle, "Failed to begin GPU copy pass");
            return new GpuCopyPass(this, passHandle);
        }

        // --- Command Buffer Actions ---

        /// <summary>
        /// Submits the recorded commands to the GPU for execution.
        /// The command buffer cannot be used after submission.
        /// </summary>
        /// <exception cref="SDLException">Thrown if submission fails.</exception>
        public void Submit()
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            SdlHost.ThrowOnFailure(
                SDL_SubmitGPUCommandBuffer(Handle),
                "Failed to submit GPU command buffer"
            );
            _isSubmittedOrCancelled = true;
        }

        /// <summary>
        /// Submits the recorded commands and acquires a fence for synchronization.
        /// The command buffer cannot be used after submission.
        /// </summary>
        /// <returns>A GpuFence object that can be queried or waited on.</returns>
        /// <exception cref="SDLException">Thrown if submission or fence acquisition fails.</exception>
        public GpuFence SubmitAndAcquireFence()
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            IntPtr fenceHandle = SDL_SubmitGPUCommandBufferAndAcquireFence(Handle);
            SdlHost.ThrowOnNull(fenceHandle, "Failed to submit command buffer and acquire fence");
            _isSubmittedOrCancelled = true;
            return new GpuFence(Device, fenceHandle); // Create the wrapper
        }

        /// <summary>
        /// Cancels the command buffer without submitting it.
        /// The command buffer cannot be used after cancellation.
        /// </summary>
        public void Cancel()
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            // SDL_CancelGPUCommandBuffer returns bool, but often cancellation doesn't fail critically.
            SDL_CancelGPUCommandBuffer(Handle);
            _isSubmittedOrCancelled = true;
        }

        // --- Uniforms & Debug ---

        /// <summary>Push vertex shader uniform data.</summary>
        public unsafe void PushVertexUniformData(uint slotIndex, ReadOnlySpan<byte> data)
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            if (data.IsEmpty)
                return;
            fixed (byte* ptr = data)
            {
                SDL_PushGPUVertexUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)data.Length);
            }
        }

        // Overload for convenience with structs
        public unsafe void PushVertexUniformData<T>(uint slotIndex, ref T data)
            where T : unmanaged
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            fixed (void* ptr = &data)
            {
                SDL_PushGPUVertexUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)sizeof(T));
            }
        }

        /// <summary>Push fragment shader uniform data.</summary>
        public unsafe void PushFragmentUniformData(uint slotIndex, ReadOnlySpan<byte> data)
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            if (data.IsEmpty)
                return;
            fixed (byte* ptr = data)
            {
                SDL_PushGPUFragmentUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)data.Length);
            }
        }

        public unsafe void PushFragmentUniformData<T>(uint slotIndex, ref T data)
            where T : unmanaged
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            fixed (void* ptr = &data)
            {
                SDL_PushGPUFragmentUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)sizeof(T));
            }
        }

        /// <summary>Push compute shader uniform data.</summary>
        public unsafe void PushComputeUniformData(uint slotIndex, ReadOnlySpan<byte> data)
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            if (data.IsEmpty)
                return;
            fixed (byte* ptr = data)
            {
                SDL_PushGPUComputeUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)data.Length);
            }
        }

        public unsafe void PushComputeUniformData<T>(uint slotIndex, ref T data)
            where T : unmanaged
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            fixed (void* ptr = &data)
            {
                SDL_PushGPUComputeUniformData(Handle, slotIndex, (IntPtr)ptr, (uint)sizeof(T));
            }
        }

        /// <summary>Pushes a debug group marker.</summary>
        public void PushDebugGroup(string name)
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            SDL_PushGPUDebugGroup(Handle, name);
        }

        /// <summary>Pops the current debug group marker.</summary>
        public void PopDebugGroup()
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            SDL_PopGPUDebugGroup(Handle);
        }

        /// <summary>Inserts a debug label.</summary>
        public void InsertDebugLabel(string text)
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            SDL_InsertGPUDebugLabel(Handle, text);
        }

        // --- Swapchain Texture Acquisition ---
        /// <summary>
        /// Acquires the next texture from the window's swapchain for rendering.
        /// This version waits if necessary.
        /// This function should only be called from the thread that created the window.
        /// </summary>
        /// <param name="window">The window whose swapchain texture to acquire.</param>
        /// <param name="swapchainTexture">Outputs the acquired swapchain texture handle.</param>
        /// <param name="width">Outputs the width of the swapchain texture.</param>
        /// <param name="height">Outputs the height of the swapchain texture.</param>
        /// <returns>True on success, false on failure (e.g., window closed or error).</returns>
        public bool WaitAndAcquireSwapchainTexture(
            Window window,
            out IntPtr swapchainTexture,
            out uint width,
            out uint height
        )
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);

            return SDL_WaitAndAcquireGPUSwapchainTexture(
                Handle,
                window.Handle,
                out swapchainTexture,
                out width,
                out height
            );
            // Consider wrapping the IntPtr in a GpuTexture? Be careful with ownership/disposal!
            // The swapchain texture handle is managed by SDL internally.
        }

        /// <summary>
        /// Acquires the next texture from the window's swapchain, if available without waiting.
        /// </summary>
        /// <param name="window">The window whose swapchain texture to acquire.</param>
        /// <param name="swapchainTexture">Outputs the acquired swapchain texture handle if successful.</param>
        /// <param name="width">Outputs the width of the swapchain texture if successful.</param>
        /// <param name="height">Outputs the height of the swapchain texture if successful.</param>
        /// <returns>True if a texture was acquired immediately, false otherwise.</returns>
        public bool TryAcquireSwapchainTexture(
            Window window,
            out IntPtr swapchainTexture,
            out uint width,
            out uint height
        )
        {
            ObjectDisposedException.ThrowIf(_isSubmittedOrCancelled, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);

            return SDL_AcquireGPUSwapchainTexture(
                Handle,
                window.Handle,
                out swapchainTexture,
                out width,
                out height
            );
        }
    }

    #endregion

    #region GPU Device

    /// <summary>
    /// Represents a GPU device, the main interface for interacting with the GPU API.
    /// </summary>
    public sealed class GpuDevice : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed = false;
        private readonly List<WeakReference<GpuResource>> _trackedResources = new(); // To potentially warn about leaks

        public IntPtr Handle =>
            _disposed ? throw new ObjectDisposedException(nameof(GpuDevice)) : _handle;
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Gets the name of the underlying GPU driver backend (e.g., "vulkan", "metal", "direct3d12").
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public string DriverName => SDL_GetGPUDeviceDriver(Handle) ?? "unknown";

        /// <summary>
        /// Creates a new GPU device.
        /// </summary>
        /// <param name="formats">The shader formats the application can provide.</param>
        /// <param name="enableDebugMode">Whether to enable backend debug layers/validation.</param>
        /// <param name="driverName">Optional name of a specific driver backend to request.</param>
        /// <exception cref="SDLException">Thrown if device creation fails.</exception>
        public GpuDevice(
            GpuShaderFormat formats,
            bool enableDebugMode = false,
            string? driverName = null
        )
        {
            _handle = SDL_CreateGPUDevice(
                (SDL_GPUShaderFormat)formats,
                enableDebugMode,
                driverName
            );
            SdlHost.ThrowOnNull(_handle, "Failed to create GPU device");
        }

        /// <summary>
        /// Creates a new GPU device using properties.
        /// </summary>
        /// <param name="properties">The properties handle created via SDL_CreateProperties().</param>
        /// <exception cref="SDLException">Thrown if device creation fails.</exception>
        /// <remarks>The caller is responsible for destroying the properties handle AFTER device creation.</remarks>
        public GpuDevice(uint properties)
        {
            _handle = SDL_CreateGPUDeviceWithProperties(properties);
            SdlHost.ThrowOnNull(_handle, "Failed to create GPU device with properties");
        }

        /// <summary>
        /// Claims a window for use with this GPU device.
        /// </summary>
        /// <remarks>
        /// This must be called before AcquireGPUSwapchainTexture is called using the window. You should only call this function from the thread that created the window. The swapchain will be created with SWAPCHAINCOMPOSITION_SDR and GPU_PRESENTMODE_VSYNC. If you want to have different swapchain parameters, you must call SetGPUSwapchainParameters after claiming the window.
        /// </remarks>
        /// <param name="window">The window to claim.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SDLException">Thrown if claiming the window fails.</exception>
        public void ClaimWindow(Window window)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            SdlHost.ThrowOnFailure(
                SDL_ClaimWindowForGPUDevice(Handle, window.Handle),
                "Failed to claim window for GPU device"
            );
        }

        /// <summary>
        /// Releases a previously claimed window from this GPU device.
        /// </summary>
        /// <param name="window">The window to release.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void ReleaseWindow(Window window)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            // SDL_ReleaseWindowFromGPUDevice returns void, assume success if handles are valid
            SDL_ReleaseWindowFromGPUDevice(Handle, window.Handle);
        }

        /// <summary>
        /// Acquires a command buffer for recording GPU commands.
        /// </summary>
        /// <returns>A new GpuCommandBuffer instance.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SDLException">Thrown if acquiring the command buffer fails.</exception>
        public GpuCommandBuffer AcquireCommandBuffer()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IntPtr cmdHandle = SDL_AcquireGPUCommandBuffer(Handle);
            SdlHost.ThrowOnNull(cmdHandle, "Failed to acquire GPU command buffer");
            return new GpuCommandBuffer(this, cmdHandle);
        }

        // --- Resource Creation ---

        /// <summary>
        /// Creates a GPU shader.
        /// </summary>
        /// <param name="createInfo">Shader creation parameters.</param>
        /// <returns>A new GpuShader instance.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SDLException">Thrown on failure.</exception>
        public unsafe GpuShader CreateShader(GpuShaderCreateInfo createInfo)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Marshal strings and code data
            IntPtr entryPointUtf8 = Marshal.StringToCoTaskMemUTF8(createInfo.EntryPoint);
            fixed (byte* codePtr = createInfo.Code)
            {
                try
                {
                    SDL_GPUShaderCreateInfo nativeInfo = createInfo.ToNative(
                        codePtr,
                        (byte*)entryPointUtf8
                    );
                    IntPtr shaderHandle = SDL_CreateGPUShader(Handle, in nativeInfo);
                    SdlHost.ThrowOnNull(shaderHandle, "Failed to create GPU shader");
                    var shader = new GpuShader(
                        this,
                        shaderHandle,
                        createInfo.Format,
                        createInfo.Stage
                    );
                    TrackResource(shader);
                    return shader;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(entryPointUtf8);
                }
            }
        }

        /// <summary>Creates a GPU buffer.</summary>
        public GpuBuffer CreateBuffer(GpuBufferUsageFlags usage, uint sizeInBytes)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var createInfo = new SDL_GPUBufferCreateInfo
            {
                usage = (SDL_GPUBufferUsageFlags)usage,
                size = sizeInBytes,
                props =
                    0 // Add props if needed
                ,
            };
            IntPtr bufferHandle = SDL_CreateGPUBuffer(Handle, in createInfo);
            SdlHost.ThrowOnNull(bufferHandle, "Failed to create GPU buffer");
            var buffer = new GpuBuffer(this, bufferHandle, sizeInBytes, usage);
            TrackResource(buffer);
            return buffer;
        }

        /// <summary>Creates a GPU texture.</summary>
        public GpuTexture CreateTexture(SDL_GPUTextureCreateInfo createInfo) // Use SDL struct directly
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IntPtr textureHandle = SDL_CreateGPUTexture(Handle, createInfo);
            SdlHost.ThrowOnNull(textureHandle, "Failed to create GPU texture");
            var texture = new GpuTexture(this, textureHandle, createInfo);
            TrackResource(texture);
            return texture;
        }

        // Convenience overload
        public GpuTexture CreateTexture2D(
            SDL_GPUTextureFormat format,
            GpuTextureUsageFlags usage,
            uint width,
            uint height,
            uint levelCount = 1,
            SDL_GPUSampleCount sampleCount = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1
        )
        {
            var info = new SDL_GPUTextureCreateInfo
            {
                type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
                format = format,
                usage = (SDL_GPUTextureUsageFlags)usage,
                width = width,
                height = height,
                layer_count_or_depth = 1,
                num_levels = levelCount,
                sample_count = sampleCount,
                props = 0,
            };
            return CreateTexture(info);
        }

        /// <summary>Creates a GPU sampler.</summary>
        public GpuSampler CreateSampler(SDL_GPUSamplerCreateInfo createInfo) // Use SDL struct directly
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IntPtr samplerHandle = SDL_CreateGPUSampler(Handle, in createInfo);
            SdlHost.ThrowOnNull(samplerHandle, "Failed to create GPU sampler");
            var sampler = new GpuSampler(this, samplerHandle, createInfo);
            TrackResource(sampler);
            return sampler;
        }

        // Convenience overload for common sampler
        public GpuSampler CreateSampler(
            SDL_GPUFilter minFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            SDL_GPUFilter magFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            SDL_GPUSamplerMipmapMode mipmapMode =
                SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
            SDL_GPUSamplerAddressMode addressModeU =
                SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            SDL_GPUSamplerAddressMode addressModeV =
                SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            SDL_GPUSamplerAddressMode addressModeW =
                SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE
        )
        {
            var info = new SDL_GPUSamplerCreateInfo
            {
                min_filter = minFilter,
                mag_filter = magFilter,
                mipmap_mode = mipmapMode,
                address_mode_u = addressModeU,
                address_mode_v = addressModeV,
                address_mode_w = addressModeW,
                max_anisotropy = 1.0f, // Default
                // Other fields default to 0/false
            };
            return CreateSampler(info);
        }

        /// <summary>Creates a GPU transfer buffer.</summary>
        public GpuTransferBuffer CreateTransferBuffer(
            SDL_GPUTransferBufferUsage usage,
            uint sizeInBytes
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var createInfo = new SDL_GPUTransferBufferCreateInfo
            {
                usage = usage,
                size = sizeInBytes,
                props = 0,
            };
            IntPtr transferHandle = SDL_CreateGPUTransferBuffer(Handle, in createInfo);
            SdlHost.ThrowOnNull(transferHandle, "Failed to create GPU transfer buffer");
            var transferBuffer = new GpuTransferBuffer(this, transferHandle, sizeInBytes, usage);
            TrackResource(transferBuffer);
            return transferBuffer;
        }

        /// <summary>Creates a GPU graphics pipeline.</summary>
        public GpuGraphicsPipeline CreateGraphicsPipeline(
            SDL_GPUGraphicsPipelineCreateInfo createInfo
        ) // Use SDL struct
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // Add checks for valid shader handles in createInfo?
            IntPtr pipelineHandle = SDL_CreateGPUGraphicsPipeline(Handle, in createInfo);
            SdlHost.ThrowOnNull(pipelineHandle, "Failed to create GPU graphics pipeline");
            var pipeline = new GpuGraphicsPipeline(this, pipelineHandle, createInfo);
            TrackResource(pipeline);
            return pipeline;
        }

        /// <summary>Creates a GPU compute pipeline.</summary>
        public unsafe GpuComputePipeline CreateComputePipeline(
            SDL_GPUComputePipelineCreateInfo createInfo
        ) // Use SDL struct
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IntPtr pipelineHandle = SDL_CreateGPUComputePipeline(Handle, createInfo);
            SdlHost.ThrowOnNull(pipelineHandle, "Failed to create GPU compute pipeline");
            var pipeline = new GpuComputePipeline(this, pipelineHandle, createInfo);
            TrackResource(pipeline);
            return pipeline;
        }

        // --- Swapchain & Sync ---

        /// <summary>Sets parameters for a window's swapchain.</summary>
        public bool SetSwapchainParameters(
            Window window,
            SDL_GPUSwapchainComposition composition,
            SDL_GPUPresentMode presentMode
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            return SDL_SetGPUSwapchainParameters(Handle, window.Handle, composition, presentMode);
        }

        /// <summary>Gets the texture format of a window's swapchain.</summary>
        public SDL_GPUTextureFormat GetSwapchainTextureFormat(Window window)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            return SDL_GetGPUSwapchainTextureFormat(Handle, window.Handle);
        }

        /// <summary>Checks if a swapchain composition mode is supported for a window.</summary>
        public bool SupportsSwapchainComposition(
            Window window,
            SDL_GPUSwapchainComposition composition
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            return SDL_WindowSupportsGPUSwapchainComposition(Handle, window.Handle, composition);
        }

        /// <summary>Checks if a present mode is supported for a window.</summary>
        public bool SupportsPresentMode(Window window, SDL_GPUPresentMode presentMode)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            return SDL_WindowSupportsGPUPresentMode(Handle, window.Handle, presentMode);
        }

        /// <summary>Waits until the GPU has finished processing all submitted commands.</summary>
        public bool WaitForIdle()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SDL_WaitForGPUIdle(Handle);
        }

        /// <summary>Waits for one or more fences to be signaled.</summary>
        public bool WaitForFences(bool waitAll, ReadOnlySpan<GpuFence> fences)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (fences.IsEmpty)
                return true;

            // Extract native handles, allocate on stack if possible
            Span<IntPtr> fenceHandles =
                fences.Length <= 128 ? stackalloc IntPtr[fences.Length] : new IntPtr[fences.Length];

            for (int i = 0; i < fences.Length; i++)
            {
                ObjectDisposedException.ThrowIf(fences[i].IsDisposed, fences[i]);
                fenceHandles[i] = fences[i].Handle;
            }

            return SDL_WaitForGPUFences(Handle, waitAll, fenceHandles, (uint)fences.Length);
        }

        /// <summary>Waits for a specific fence.</summary>
        public bool WaitForFence(GpuFence fence)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(fence);
            ObjectDisposedException.ThrowIf(fence.IsDisposed, fence);
            Span<IntPtr> fenceHandle = stackalloc IntPtr[1];
            fenceHandle[0] = fence.Handle;
            return SDL_WaitForGPUFences(Handle, true, fenceHandle, 1); // waitAll = true for single fence
        }

        /// <summary>Waits for the swapchain presentation associated with a window to complete.</summary>
        public bool WaitForSwapchain(Window window)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(window);
            ObjectDisposedException.ThrowIf(window.IsDisposed, window);
            return SDL_WaitForGPUSwapchain(Handle, window.Handle);
        }

        /// <summary> Sets the number of frames the CPU is allowed to queue ahead of the GPU.</summary>
        public bool SetAllowedFramesInFlight(uint framesInFlight)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SDL_SetGPUAllowedFramesInFlight(Handle, framesInFlight);
        }

        // --- Support Queries ---

        /// <summary>Checks if the device supports the given shader formats.</summary>
        public bool SupportsShaderFormats(GpuShaderFormat formats)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // SDL_GPUSupportsShaderFormats takes an optional driver name, pass null here
            return SDL_GPUSupportsShaderFormats((SDL_GPUShaderFormat)formats, null);
        }

        /// <summary>Checks if a texture format is supported for a given type and usage.</summary>
        public bool SupportsTextureFormat(
            SDL_GPUTextureFormat format,
            SDL_GPUTextureType type,
            GpuTextureUsageFlags usage
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SDL_GPUTextureSupportsFormat(
                Handle,
                format,
                type,
                (SDL_GPUTextureUsageFlags)usage
            );
        }

        /// <summary>Checks if a sample count is supported for a given texture format.</summary>
        public bool SupportsSampleCount(SDL_GPUTextureFormat format, SDL_GPUSampleCount sampleCount)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SDL_GPUTextureSupportsSampleCount(Handle, format, sampleCount);
        }

        // --- Internal Resource Management ---
        private void TrackResource(GpuResource resource)
        {
            lock (_trackedResources)
            {
                // Clean up dead references occasionally
                if (_trackedResources.Count > 0 && _trackedResources.Count % 32 == 0) // Arbitrary check interval
                {
                    _trackedResources.RemoveAll(wr => !wr.TryGetTarget(out _));
                }
                _trackedResources.Add(new WeakReference<GpuResource>(resource));
            }
        }

        // These are called by the GpuResource Dispose methods
        internal void ReleaseShaderInternal(IntPtr shaderHandle) =>
            SDL_ReleaseGPUShader(Handle, shaderHandle);

        internal void ReleaseBufferInternal(IntPtr bufferHandle) =>
            SDL_ReleaseGPUBuffer(Handle, bufferHandle);

        internal void ReleaseTextureInternal(IntPtr textureHandle) =>
            SDL_ReleaseGPUTexture(Handle, textureHandle);

        internal void ReleaseSamplerInternal(IntPtr samplerHandle) =>
            SDL_ReleaseGPUSampler(Handle, samplerHandle);

        internal void ReleaseGraphicsPipelineInternal(IntPtr pipelineHandle) =>
            SDL_ReleaseGPUGraphicsPipeline(Handle, pipelineHandle);

        internal void ReleaseComputePipelineInternal(IntPtr pipelineHandle) =>
            SDL_ReleaseGPUComputePipeline(Handle, pipelineHandle);

        internal void ReleaseTransferBufferInternal(IntPtr transferHandle) =>
            SDL_ReleaseGPUTransferBuffer(Handle, transferHandle);

        internal void ReleaseFenceInternal(IntPtr fenceHandle) =>
            SDL_ReleaseGPUFence(Handle, fenceHandle);

        // --- IDisposable Implementation ---

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // Check for leaked resources (optional)
                    lock (_trackedResources)
                    {
                        _trackedResources.RemoveAll(wr => !wr.TryGetTarget(out _)); // Clean up dead refs first
                        if (_trackedResources.Count > 0)
                        {
                            Console.Error.WriteLine(
                                $"Warning: GpuDevice disposed with {_trackedResources.Count} tracked resources potentially not released:"
                            );
                            foreach (var weakRef in _trackedResources)
                            {
                                if (weakRef.TryGetTarget(out var resource))
                                {
                                    Console.Error.WriteLine(
                                        $" - {resource.GetType().Name} ({resource.Handle})"
                                    );
                                }
                            }
                            _trackedResources.Clear();
                        }
                    }
                }

                // Free unmanaged resources
                if (_handle != IntPtr.Zero)
                {
                    // Make sure all associated windows are released? SDL might handle this.
                    SDL_DestroyGPUDevice(_handle); // Destroy the native device
                    _handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~GpuDevice()
        {
            Dispose(disposing: false);
        }
    }

    #endregion

    #region Core and Exceptions

    /// <summary>
    /// Represents an error originating from the SDL library.
    /// </summary>
    public class SDLException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with the last SDL error message.
        /// </summary>
        public SDLException()
            : base(message: GetSDLError()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SDLException(string message)
            : base(message: message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public SDLException(string message, Exception inner)
            : base(message: message, innerException: inner) { }

        /// <summary>
        /// Helper to get the last SDL error message.
        /// </summary>
        /// <returns>The last error message from SDL.</returns>
        internal static string GetSDLError() // Made internal as it's primarily for wrapper use
        {
            // Use the marshaller defined in SDL3.Core.cs
            // Note: SDL_GetError() returns an SDL-owned string,
            // so we use the SDLOwnedStringMarshaller implicitly if available,
            // otherwise Marshal.PtrToStringUTF8 is a common way.
            // Assuming the bindings handle marshalling correctly.
            string? error = SDL_GetError();
            return string.IsNullOrEmpty(value: error) ? "Unknown SDL error." : error;
        }
    }

    /// <summary>
    /// Specifies SDL subsystems for initialization.
    /// </summary>
    [Flags]
    public enum SdlSubSystem : uint
    {
        None = 0,
        Timer = SDL_InitFlags.SDL_INIT_TIMER,
        Audio = SDL_InitFlags.SDL_INIT_AUDIO,
        Video = SDL_InitFlags.SDL_INIT_VIDEO,
        Joystick = SDL_InitFlags.SDL_INIT_JOYSTICK,
        Haptic = SDL_InitFlags.SDL_INIT_HAPTIC,
        Gamepad = SDL_InitFlags.SDL_INIT_GAMEPAD,
        Events = SDL_InitFlags.SDL_INIT_EVENTS,
        Sensor = SDL_InitFlags.SDL_INIT_SENSOR,
        Camera = SDL_InitFlags.SDL_INIT_CAMERA,
        Everything = Timer | Audio | Video | Joystick | Haptic | Gamepad | Events | Sensor | Camera,
    }

    /// <summary>
    /// Delegate for application initialization.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True on success, false on failure (will cause application exit).</returns>
    public delegate bool AppInitHandler(string[] args);

    /// <summary>
    /// Delegate for handling application events.
    /// </summary>
    /// <param name="eventArgs">The event arguments, or null if the event type is unhandled by the wrapper.</param>
    /// <returns>True to continue running, false to request application exit.</returns>
    public delegate bool AppEventHandler(SdlEventArgs? eventArgs);

    /// <summary>
    /// Delegate for the application's main update/iteration logic.
    /// </summary>
    /// <returns>True to continue running, false to request application exit.</returns>
    public delegate bool AppUpdateHandler();

    /// <summary>
    /// Delegate for application cleanup before exiting.
    /// </summary>
    public delegate void AppQuitHandler();

    /// <summary>
    /// Provides static methods for initializing, shutting down,
    /// and managing core SDL functionality.
    /// </summary>
    public static class SdlHost
    {
        private class AppContext
        {
            public AppInitHandler? UserInit { get; set; }
            public AppUpdateHandler? UserUpdate { get; set; }
            public AppEventHandler? UserEvent { get; set; }
            public AppQuitHandler? UserQuit { get; set; }
            public Exception? LastException { get; set; } // To capture exceptions in callbacks
        }

        private static readonly SDL_AppInit_func _nativeInit = NativeAppInit;
        private static readonly SDL_AppIterate_func _nativeIterate = NativeAppIterate;
        private static readonly unsafe SDL_AppEvent_func _nativeEvent = NativeAppEvent;
        private static readonly SDL_AppQuit_func _nativeQuit = NativeAppQuit;
        private static GCHandle _pendingAppHandle;

        private static readonly Lock _runLock = new(); // Assuming Lock is a valid locking mechanism

        // Static native callback wrappers
        private static SDL_AppResult NativeAppInit(IntPtr appstatePtrRef, int argc, IntPtr argv)
        {
            // This callback uses the temporary static handle mechanism to establish the state pointer with SDL.
            GCHandle handle = default;
            AppContext? context = null;
            try
            {
                // Step 1: Retrieve the handle from the static field set in RunApplication
                if (!_pendingAppHandle.IsAllocated)
                {
                    SDL_LogError(
                        category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                        fmt: "NativeAppInit: No pending AppContext handle found!"
                    );
                    Console.Error.WriteLine(
                        value: "NativeAppInit: No pending AppContext handle found!"
                    );
                    return SDL_AppResult.SDL_APP_FAILURE;
                }

                handle = _pendingAppHandle;
                context = handle.Target as AppContext;

                if (context == null)
                {
                    SDL_LogError(
                        category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                        fmt: "NativeAppInit: Failed to get AppContext from pending handle."
                    );
                    Console.Error.WriteLine(
                        value: "NativeAppInit: Failed to get AppContext from pending handle."
                    );
                    _pendingAppHandle = default; // Clear invalid static handle
                    return SDL_AppResult.SDL_APP_FAILURE;
                }

                // Step 2: Write the *actual* handle pointer back to SDL via the appstatePtrRef (void**)
                // This tells SDL which pointer to use for subsequent Iterate/Event/Quit calls.
                IntPtr actualHandlePtr = GCHandle.ToIntPtr(value: handle);
                Marshal.WriteIntPtr(ptr: appstatePtrRef, val: actualHandlePtr);
                Console.WriteLine(
                    value: $"NativeAppInit: Thread {Environment.CurrentManagedThreadId}: Set SDL app state pointer to {actualHandlePtr:X} via appstatePtrRef {appstatePtrRef:X}."
                );

                // Step 3: Clear the temporary static handle now that SDL knows the real one.
                _pendingAppHandle = default;

                // Step 4: Proceed with user initialization logic
                if (context.UserInit == null)
                {
                    Console.WriteLine(
                        value: "NativeAppInit: No user init handler provided, continuing."
                    );
                    return SDL_AppResult.SDL_APP_SUCCESS; // Treat as success if no user init provided
                }

                // Marshal args (same as before)
                string[] managedArgs = Array.Empty<string>();
                if (argc > 0 && argv != IntPtr.Zero)
                {
                    managedArgs = new string[argc];
                    unsafe
                    {
                        byte** nativeArgs = (byte**)argv;
                        for (int i = 0; i < argc; i++)
                        {
                            // Assuming UTF8, adjust if needed
                            managedArgs[i] =
                                Marshal.PtrToStringUTF8(ptr: (IntPtr)nativeArgs[i]) ?? string.Empty;
                        }
                    }
                }
                Console.WriteLine(
                    value: $"NativeAppInit: Calling UserInit with {managedArgs.Length} args..."
                );
                // Call user delegate
                bool success = context.UserInit(args: managedArgs);
                Console.WriteLine(value: $"NativeAppInit: UserInit returned {success}.");
                return success ? SDL_AppResult.SDL_APP_CONTINUE : SDL_AppResult.SDL_APP_FAILURE;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(value: $"Exception in NativeAppInit: {ex}");
                SDL_LogError(
                    category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                    fmt: $"Exception in NativeAppInit: {ex.Message}"
                );
                if (context != null)
                    context.LastException = ex; // Store exception
                // Ensure static handle is cleared on error
                if (_pendingAppHandle.IsAllocated)
                    _pendingAppHandle = default;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure
            }
        }

        private static SDL_AppResult NativeAppIterate(IntPtr appstate)
        {
            AppContext? context = null;
            try
            {
                // Retrieve context from the appstate pointer provided by SDL
                if (appstate == IntPtr.Zero)
                    return SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(value: appstate); // Use the pointer SDL gives us now
                if (!handle.IsAllocated)
                    return SDL_AppResult.SDL_APP_FAILURE; // Handle wasn't set or was freed early

                context = handle.Target as AppContext;
                if (context?.UserUpdate == null)
                    return SDL_AppResult.SDL_APP_CONTINUE; // No user update, just continue

                // Call user delegate
                bool continueRunning = context.UserUpdate();
                return continueRunning
                    ? SDL_AppResult.SDL_APP_CONTINUE
                    : SDL_AppResult.SDL_APP_SUCCESS; // Return SUCCESS to quit cleanly
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(value: $"Exception in AppUpdate: {ex}");
                if (context != null)
                    context.LastException = ex;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure on exception
            }
        }

        private static unsafe SDL_AppResult NativeAppEvent(IntPtr appstate, SDL_Event* evt)
        {
            AppContext? context = null;
            try
            {
                // Retrieve context from the appstate pointer provided by SDL
                if (appstate == IntPtr.Zero)
                    return SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(value: appstate);
                if (!handle.IsAllocated)
                    return SDL_AppResult.SDL_APP_FAILURE;

                context = handle.Target as AppContext;
                if (context?.UserEvent == null)
                    return SDL_AppResult.SDL_APP_CONTINUE; // No user event handler

                // Map event (MapEvent takes SDL_Event by value, so dereference)
                SdlEventArgs? managedEvent = Events.MapEvent(sdlEvent: *evt);

                // Call user delegate
                bool continueRunning = context.UserEvent(eventArgs: managedEvent);
                return continueRunning
                    ? SDL_AppResult.SDL_APP_CONTINUE
                    : SDL_AppResult.SDL_APP_SUCCESS; // Return SUCCESS to quit cleanly
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(value: $"Exception in AppEvent: {ex}");
                if (context != null)
                    context.LastException = ex;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure on exception
            }
        }

        private static void NativeAppQuit(IntPtr appstate, SDL_AppResult result)
        {
            AppContext? context = null;
            GCHandle handle = default;
            bool handleIsValid = false;
            try
            {
                Console.WriteLine(
                    value: $"NativeAppQuit: Received appstate IntPtr value: {appstate.ToString(format: "X")}. Result code: {result}"
                ); // Log as Hex

                // Retrieve context one last time using the appstate passed by SDL
                if (appstate != IntPtr.Zero)
                {
                    handle = GCHandle.FromIntPtr(value: appstate);
                    if (handle.IsAllocated)
                    {
                        handleIsValid = true; // Mark that we found a valid handle via appstate
                        context = handle.Target as AppContext;
                    }
                    else
                    {
                        SDL.SDL_LogWarn(
                            category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            fmt: "NativeAppQuit: Received appstate pointer, but GCHandle was not allocated."
                        );
                    }
                }
                else
                {
                    SDL.SDL_LogWarn(
                        category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                        fmt: "NativeAppQuit: Received null appstate pointer."
                    );
                }

                context?.UserQuit?.Invoke(); // Call user quit delegate if provided and context was retrieved
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(value: $"Exception in AppQuit: {ex}");
                SDL.SDL_LogError(
                    category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                    fmt: $"Exception in NativeAppQuit: {ex.Message}"
                );
                // Don't store exception here as the app is already quitting
            }
            finally
            {
                // Clean up SDL subsystems (your existing logic is fine here)
                if (_isInitialized)
                {
                    SDL_Quit();
                    _isInitialized = false;
                    Console.WriteLine(value: "SDL Quit from NativeAppQuit.");
                }

                // Free the GCHandle that was keeping the context alive.
                // This handle *MUST* be the one passed via appstate.
                if (handleIsValid) // Only free if we successfully got a valid handle from appstate
                {
                    try
                    {
                        handle.Free();
                        // Console.WriteLine("App GCHandle Freed in NativeAppQuit.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Handle case where handle might have been freed elsewhere (shouldn't happen ideally) or was invalid
                        Console.Error.WriteLine(
                            value: $"Warning: Attempted to free GCHandle in NativeAppQuit, but it was invalid or already freed: {ex.Message}"
                        );
                        SDL.SDL_LogWarn(
                            category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            fmt: "NativeAppQuit: Attempted to free GCHandle, but it was invalid or already freed."
                        );
                    }
                }
                // Removed the fallback for _pendingAppHandle as it's no longer used.
            }
        }

        private static bool _isInitialized = false;
        private static readonly Lock _initLock = new();

        /// <summary>
        /// Gets a value indicating whether any SDL subsystems have been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes SDL subsystems using the wrapper enum.
        /// Must be called before using most SDL functions.
        /// </summary>
        /// <param name="flags">The subsystems to initialize.</param>
        /// <exception cref="SDLException">Thrown if SDL fails to initialize.</exception>
        public static void Init(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                SDL_InitFlags sdlFlags = (SDL_InitFlags)flags; // Cast to SDL enum
                if (_isInitialized && SDL_WasInit(flags: sdlFlags) == sdlFlags)
                {
                    return;
                }

                if (!SDL_Init(flags: sdlFlags)) // Call SDL with casted flags
                {
                    throw new SDLException("Failed to initialize SDL subsystems.");
                }
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Initializes specific SDL subsystems using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to initialize.</param>
        /// <exception cref="SDLException">Thrown if SDL fails to initialize the subsystem.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static void InitSubSystem(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException(
                        "SDL must be initialized with Init() first."
                    );
                }
                SDL_InitFlags sdlFlags = (SDL_InitFlags)flags; // Cast to SDL enum
                if (SDL_InitSubSystem(flags: sdlFlags)) // Call SDL with casted flags
                {
                    throw new SDLException($"Failed to initialize SDL subsystem: {flags}.");
                }
            }
        }

        /// <summary>
        /// Shuts down specific SDL subsystems using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to shut down.</param>
        public static void QuitSubSystem(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    SDL_QuitSubSystem(flags: (SDL_InitFlags)flags); // Cast to SDL enum
                }
            }
        }

        /// <summary>
        /// Cleans up all initialized SDL subsystems.
        /// Call this when your application is exiting.
        /// </summary>
        public static void Quit()
        {
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    // Clean up other resources if necessary before SDL_Quit
                    Events.Quit(); // Ensure event system is cleaned up if needed
                    SDL_Quit();
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Runs the SDL application using the provided lifecycle callbacks.
        /// This method takes over the main loop and manages SDL initialization and shutdown.
        /// </summary>
        /// <param name="init">Initialization callback.</param>
        /// <param name="update">Main loop iteration callback.</param>
        /// <param name="eventHandler">Event processing callback.</param>
        /// <param name="quit">Cleanup callback.</param>
        /// <param name="args">Command line arguments passed to the init callback.</param>
        /// <returns>Exit code (0 for success, non-zero for failure).</returns>
        public static unsafe int RunApplication(
            AppInitHandler init,
            AppUpdateHandler update,
            AppEventHandler eventHandler,
            AppQuitHandler quit,
            string[]? args = null
        )
        {
            Init(flags: SdlSubSystem.Video);
            // Ensure RunApplication is not called concurrently if that's an issue
            if (!_runLock.TryEnter()) // Assuming TryLock exists
            {
                throw new InvalidOperationException("SdlHost.RunApplication is already running.");
            }

            // NOTE: SDL_EnterAppMainCallbacks handles SDL_Init and SDL_Quit internally.
            // We do NOT call SdlHost.Init() or SdlHost.Quit() manually here.

            // Prepare context
            var context = new AppContext
            {
                UserInit = init,
                UserUpdate = update,
                UserEvent = eventHandler,
                UserQuit = quit,
            };

            GCHandle contextHandle = default; // Use default initialization
            IntPtr argvPtr = IntPtr.Zero;
            int argc = 0;
            int result = -1; // Default to error

            try
            {
                // Allocate handle for the context object
                contextHandle = GCHandle.Alloc(value: context, type: GCHandleType.Normal);
                IntPtr contextHandlePtr = GCHandle.ToIntPtr(value: contextHandle);
                Console.WriteLine(
                    value: $"RunApplication: Thread {Environment.CurrentManagedThreadId}: Allocating GCHandle {contextHandlePtr:X} for AppContext."
                );

                // --- CRITICAL STEP: Set the static handle for NativeAppInit to pick up ---
                if (_pendingAppHandle.IsAllocated)
                {
                    // This should not happen if RunApplication is single-threaded as enforced by _runLock
                    Console.Error.WriteLine(
                        value: "RunApplication Warning: _pendingAppHandle was already allocated!"
                    );
                    _pendingAppHandle.Free(); // Attempt cleanup just in case
                }
                _pendingAppHandle = contextHandle;
                Console.WriteLine(
                    value: $"RunApplication: Thread {Environment.CurrentManagedThreadId}: Assigned GCHandle to _pendingAppHandle. IsAllocated = {_pendingAppHandle.IsAllocated}"
                );
                // --- End Critical Step ---

                // Marshal command line arguments (if provided) - same as before
                if (args?.Length > 0)
                {
                    argc = args.Length;
                    // Allocate unmanaged memory for the array of pointers (**char)
                    argvPtr = Marshal.AllocHGlobal(cb: IntPtr.Size * argc);
                    byte** argv = (byte**)argvPtr; // Treat as byte** for UTF8 marshalling
                    for (int i = 0; i < argc; i++)
                    {
                        // Allocate memory for each string and copy (null-terminated UTF8)
                        byte[] utf8Bytes = Encoding.UTF8.GetBytes(s: args[i] + '\0');
                        IntPtr argPtr = Marshal.AllocHGlobal(cb: utf8Bytes.Length);
                        Marshal.Copy(
                            source: utf8Bytes,
                            startIndex: 0,
                            destination: argPtr,
                            length: utf8Bytes.Length
                        );
                        argv[i] = (byte*)argPtr;
                    }
                    Console.WriteLine(value: $"RunApplication: Marshalled {argc} arguments.");
                }
                else
                {
                    Console.WriteLine(value: "RunApplication: No arguments to marshal.");
                }

                Console.WriteLine(value: "RunApplication: Entering SDL_EnterAppMainCallbacks...");
                // Call SDL's main entry point with the static delegate references
                result = SDL_EnterAppMainCallbacks(
                    argc: argc,
                    argv: argvPtr,
                    appinit: _nativeInit,
                    appiter: _nativeIterate,
                    appevent: _nativeEvent,
                    appquit: _nativeQuit
                );
                Console.WriteLine(
                    value: $"RunApplication: SDL_EnterAppMainCallbacks returned {result}."
                );

                // Check if an exception occurred in a callback (stored in context)
                if (context.LastException != null)
                {
                    Console.Error.WriteLine(
                        value: "RunApplication: Exiting due to unhandled exception in callback."
                    );
                    // Optionally re-throw or handle differently
                    // throw new Exception("Exception occurred in SDL callback.", context.LastException);
                    result =
                        context.LastException.HResult != 0 ? context.LastException.HResult : -1; // Use HResult as exit code if available
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    value: $"RunApplication: Exception during setup or execution: {ex}"
                );
                SDL_LogError(
                    category: (int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                    fmt: $"RunApplication Exception: {ex.Message}"
                );
                result = ex.HResult != 0 ? ex.HResult : -1;
            }
            finally
            {
                Console.WriteLine(value: "RunApplication: Entering finally block.");
                // Free marshaled arguments (same as before)
                if (argvPtr != IntPtr.Zero)
                {
                    Console.WriteLine(
                        value: $"RunApplication: Freeing {argc} marshalled arguments..."
                    );
                    byte** argv = (byte**)argvPtr;
                    for (int i = 0; i < argc; i++)
                    {
                        if (argv[i] != null)
                            Marshal.FreeHGlobal(hglobal: (IntPtr)argv[i]);
                    }
                    Marshal.FreeHGlobal(hglobal: argvPtr);
                    Console.WriteLine(value: "RunApplication: Finished freeing arguments.");
                }

                // Free the GCHandle for the AppContext
                if (contextHandle.IsAllocated)
                {
                    IntPtr freedHandlePtr = GCHandle.ToIntPtr(value: contextHandle);
                    contextHandle.Free();
                    Console.WriteLine(
                        value: $"RunApplication: Freed GCHandle {freedHandlePtr:X}. IsAllocated = {contextHandle.IsAllocated}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        value: "RunApplication: Context GCHandle was not allocated or already freed."
                    );
                }

                // Ensure pending handle is cleared if something went wrong very early
                if (_pendingAppHandle.IsAllocated)
                {
                    Console.Error.WriteLine(
                        value: "RunApplication Warning: _pendingAppHandle was still allocated in finally block! Clearing."
                    );
                    _pendingAppHandle.Free();
                    _pendingAppHandle = default;
                }

                // Release the lock
                _runLock.Exit(); // Assuming Unlock exists

                Console.WriteLine(value: "RunApplication: Exiting finally block.");
            }

            return result; // Return the exit code from SDL or based on exceptions
        }

        /// <summary>
        /// Checks if specific SDL subsystems have been initialized using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to check.</param>
        /// <returns>The flags for the subsystems that are currently initialized.</returns>
        public static SdlSubSystem WasInit(SdlSubSystem flags) // Use wrapper enum
        {
            // Cast wrapper enum to SDL enum for the call, then cast result back
            return (SdlSubSystem)SDL_WasInit(flags: (SDL_InitFlags)flags);
        }

        /// <summary>
        /// Gets the last error message from SDL.
        /// </summary>
        /// <returns>The error message.</returns>
        public static string GetError() => SDLException.GetSDLError();

        /// <summary>
        /// Clears the last SDL error message.
        /// </summary>
        public static void ClearError() => SDL_ClearError();

        /// <summary>
        /// Throws an SDLException if the result indicates failure.
        /// Assumes SDL functions return 0 (SDL_FALSE) on success and non-zero (SDL_TRUE) on failure.
        /// </summary>
        /// <param name="result">The SDL_Bool result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static void ThrowOnFailure(SDLBool result, string message)
        {
            if (!result) // SDLBool is false on error
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }

        /// <summary>
        /// Throws an SDLException if the pointer is null (IntPtr.Zero).
        /// </summary>
        /// <param name="ptr">The pointer result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static void ThrowOnNull(IntPtr ptr, string message)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }

        /// <summary>
        /// Throws an SDLException if the pointer is null (IntPtr.Zero).
        /// </summary>
        /// <param name="ptr">The pointer result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static unsafe void ThrowOnNull(void* ptr, string message)
        {
            if (ptr == null)
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }
    }

    #endregion

    #region Helper Structs

    // Helper struct for Point (can be replaced with System.Drawing.Point if preferred,
    // but keeping it simple here to avoid extra dependencies).
    public struct Point(int x, int y)
    {
        public int X = x;
        public int Y = y;

        public override readonly string ToString() => $"({X}, {Y})";
    }

    // Helper struct for Float Point
    public struct FPoint(float x, float y)
    {
        public float X = x;
        public float Y = y;

        public override readonly string ToString() => $"({X:F2}, {Y:F2})";

        public static implicit operator SDL_FPoint(FPoint p)
        {
            return new SDL_FPoint { x = p.X, y = p.Y };
        }

        public static implicit operator FPoint(SDL_FPoint p)
        {
            return new FPoint(p.x, p.y);
        }
    }

    // Helper struct for Rect
    public struct Rect(int x, int y, int w, int h)
    {
        public int X = x;
        public int Y = y;
        public int W = w;
        public int H = h;

        public override readonly string ToString() => $"({X}, {Y}, {W}, {H})";

        public static implicit operator SDL_Rect(Rect r)
        {
            return new SDL_Rect
            {
                x = r.X,
                y = r.Y,
                w = r.W,
                h = r.H,
            };
        }

        public static implicit operator Rect(SDL_Rect r)
        {
            return new Rect(r.x, r.y, r.w, r.h);
        }
    }

    // Helper struct for Float Rect
    public struct FRect(float x, float y, float w, float h)
    {
        public float X = x;
        public float Y = y;
        public float W = w;
        public float H = h;

        public override readonly string ToString() => $"({X:F2}, {Y:F2}, {W:F2}, {H:F2})";

        public static implicit operator SDL_FRect(FRect r)
        {
            return new SDL_FRect
            {
                x = r.X,
                y = r.Y,
                w = r.W,
                h = r.H,
            };
        }

        public static implicit operator FRect(SDL_FRect r)
        {
            return new FRect(r.x, r.y, r.w, r.h);
        }
    }

    // Helper struct for Color
    public struct Color(byte r, byte g, byte b, byte a = 255) // Default alpha to opaque
    {
        public byte R = r;
        public byte G = g;
        public byte B = b;
        public byte A = a;

        public static implicit operator SDL_Color(Color c)
        {
            return new SDL_Color
            {
                r = c.R,
                g = c.G,
                b = c.B,
                a = c.A,
            };
        }

        public static implicit operator Color(SDL_Color c)
        {
            return new Color(c.r, c.g, c.b, c.a);
        }

        public static implicit operator Color(System.Drawing.Color c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        public static implicit operator System.Drawing.Color(Color c)
        {
            return System.Drawing.Color.FromArgb(alpha: c.A, red: c.R, green: c.G, blue: c.B);
        }

        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 255, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Magenta = new(255, 0, 255);
        public static readonly Color Cyan = new(0, 255, 255);
        public static readonly Color Transparent = new(0, 0, 0, 0);
    }

    // Helper struct for Float Color
    public struct FColor(float r, float g, float b, float a = 1.0f) // Default alpha to opaque
    {
        public float R = r;
        public float G = g;
        public float B = b;
        public float A = a;

        public static implicit operator SDL_FColor(FColor c)
        {
            return new SDL_FColor
            {
                r = c.R,
                g = c.G,
                b = c.B,
                a = c.A,
            };
        }

        public static implicit operator FColor(SDL_FColor c)
        {
            return new FColor(c.r, c.g, c.b, c.a);
        }

        public static readonly FColor Black = new(0f, 0f, 0f);
        public static readonly FColor White = new(1f, 1f, 1f);
        public static readonly FColor Red = new(1f, 0f, 0f);
        public static readonly FColor Green = new(0f, 1f, 0f);
        public static readonly FColor Blue = new(0f, 0f, 1f);
        public static readonly FColor Yellow = new(1f, 1f, 0f);
        public static readonly FColor Magenta = new(1f, 0f, 1f);
        public static readonly FColor Cyan = new(0f, 1f, 1f);
        public static readonly FColor Transparent = new(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Represents a vertex used in geometry rendering.
    /// </summary>
    public struct Vertex(FPoint position, FColor color, FPoint texCoord)
    {
        public FPoint Position = position;
        public FColor Color = color;
        public FPoint TexCoord = texCoord;

        public static implicit operator SDL_Vertex(Vertex v)
        {
            return new SDL_Vertex
            {
                position = v.Position,
                color = v.Color,
                tex_coord = v.TexCoord,
            };
        }

        public static implicit operator Vertex(SDL_Vertex v)
        {
            return new Vertex(v.position, v.color, v.tex_coord);
        }
    }

    #endregion

    #region Window

    /// <summary>
    /// Specifies window creation flags.
    /// </summary>
    [Flags]
    public enum WindowFlags : ulong // Matches SDL_WindowFlags underlying type
    {
        None = 0,
        Fullscreen = SDL_WindowFlags.SDL_WINDOW_FULLSCREEN,
        OpenGL = SDL_WindowFlags.SDL_WINDOW_OPENGL,
        Occluded = SDL_WindowFlags.SDL_WINDOW_OCCLUDED, // Read-only state flag
        Hidden = SDL_WindowFlags.SDL_WINDOW_HIDDEN,
        Borderless = SDL_WindowFlags.SDL_WINDOW_BORDERLESS,
        Resizable = SDL_WindowFlags.SDL_WINDOW_RESIZABLE,
        Minimized = SDL_WindowFlags.SDL_WINDOW_MINIMIZED,
        Maximized = SDL_WindowFlags.SDL_WINDOW_MAXIMIZED,
        MouseGrabbed = SDL_WindowFlags.SDL_WINDOW_MOUSE_GRABBED,
        InputFocus = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS, // Read-only state flag
        MouseFocus = SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS, // Read-only state flag
        External = SDL_WindowFlags.SDL_WINDOW_EXTERNAL,
        Modal = SDL_WindowFlags.SDL_WINDOW_MODAL,
        HighPixelDensity = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY,
        MouseCapture = SDL_WindowFlags.SDL_WINDOW_MOUSE_CAPTURE, // Renamed from MOUSE_RELATIVE_MODE? Check SDL3 docs. Assuming this is intended capture.
        AlwaysOnTop = SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP,
        Utility = SDL_WindowFlags.SDL_WINDOW_UTILITY,
        Tooltip = SDL_WindowFlags.SDL_WINDOW_TOOLTIP,
        PopupMenu = SDL_WindowFlags.SDL_WINDOW_POPUP_MENU,
        KeyboardGrabbed = SDL_WindowFlags.SDL_WINDOW_KEYBOARD_GRABBED, // Read-only state flag
        Vulkan = SDL_WindowFlags.SDL_WINDOW_VULKAN,
        Metal = SDL_WindowFlags.SDL_WINDOW_METAL,
        Transparent = SDL_WindowFlags.SDL_WINDOW_TRANSPARENT,
        NotFocusable = SDL_WindowFlags.SDL_WINDOW_NOT_FOCUSABLE,
        // SDL_WINDOW_MOUSE_RELATIVE_MODE (0x8000) might need separate handling or a distinct flag if different from MouseCapture
    }

    /// <summary>
    /// Specifies the type of a window event.
    /// </summary>
    public enum WindowEventType
    {
        None, // Default
        Shown,
        Hidden,
        Exposed, // Window needs to be redrawn
        Moved,
        Resized, // Window size changed by user/system
        PixelSizeChanged, // Window pixel size changed (e.g., DPI change)
        Minimized,
        Maximized,
        Restored,
        MouseEnter, // Mouse entered window
        MouseLeave, // Mouse left window
        FocusGained,
        FocusLost,
        CloseRequested, // User requested window close (e.g., clicked X)
        HitTest, // Requires custom hit test callback
        IccProfileChanged,
        DisplayChanged, // Window moved to a different display
        DisplayScaleChanged, // DPI scale changed
        SafeAreaChanged,
        Occluded, // Window obscured
        EnterFullscreen,
        LeaveFullscreen,
        Destroyed, // Window was destroyed
        HdrStateChanged,
        // Note: MetalViewResized might be too specific, handled by Resized/PixelSizeChanged?
    }

    /// <summary>
    /// Represents an SDL Window.
    /// </summary>
    public class Window : IDisposable
    {
        private IntPtr _windowPtr;
        private bool _disposed = false;
        public bool IsDisposed
        {
            get => _disposed;
        }

        /// <summary>
        /// Gets the native SDL window handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Window)) : _windowPtr;
            }
        }

        /// <summary>
        /// Gets the ID of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public uint Id
        {
            get
            {
                return _disposed
                    ? throw new ObjectDisposedException(nameof(Window))
                    : SDL_GetWindowID(window: _windowPtr);
            }
        }

        /// <summary>
        /// Creates a new SDL window using wrapper flags.
        /// SdlSubSystem.Video must be initialized before calling this.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="flags">Window creation flags (using wrapper enum).</param>
        /// <exception cref="SDLException">Thrown if the window cannot be created.</exception>
        /// <exception cref="InvalidOperationException">Thrown if SdlSubSystem.Video is not initialized.</exception>
        public Window(string title, int width, int height, WindowFlags flags = WindowFlags.None) // Use wrapper enum
        {
            if ((SdlHost.WasInit(flags: SdlSubSystem.Video) & SdlSubSystem.Video) == 0)
            {
                throw new InvalidOperationException(
                    "SdlSubSystem.Video must be initialized before creating a window."
                );
            }

            // Cast wrapper flags to SDL flags for the underlying call
            _windowPtr = SDL_CreateWindow(
                title: title,
                w: width,
                h: height,
                flags: (SDL_WindowFlags)flags
            );
            SdlHost.ThrowOnNull(ptr: _windowPtr, message: "Failed to create window");
        }

        /// <summary>
        /// Creates a new SDL window using properties.
        /// SDL_INIT_VIDEO must be initialized before calling this.
        /// </summary>
        /// <param name="properties">The properties handle created via SDL_CreateProperties().</param>
        /// <exception cref="SDLException">Thrown if the window cannot be created.</exception>
        /// <exception cref="InvalidOperationException">Thrown if SDL_INIT_VIDEO is not initialized.</exception>
        /// <remarks>The caller is responsible for destroying the properties handle AFTER the window is created.</remarks>
        public Window(uint properties)
        {
            if ((SdlHost.WasInit(flags: SdlSubSystem.Video) & SdlSubSystem.Video) == 0)
            {
                throw new InvalidOperationException(
                    "SDL_INIT_VIDEO must be initialized before creating a window."
                );
            }
            _windowPtr = SDL_CreateWindowWithProperties(props: properties);
            SdlHost.ThrowOnNull(
                ptr: _windowPtr,
                message: "Failed to create window with properties"
            );
        }

        // Internal constructor for wrapping an existing handle (e.g., from GetWindowFromID)
        // Be careful with ownership when using this.
        internal Window(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(handle), "Window handle cannot be null.");
            }
            _windowPtr = handle;
            // Assume the handle is valid and owned elsewhere, so don't destroy in Dispose
            // This might need adjustment based on specific use cases. Consider adding an 'owned' flag.
            _disposed = true; // Mark as disposed immediately to prevent SDL_DestroyWindow call
        }

        // --- Window Properties ---

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public string Title
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                // SDL_GetWindowTitle returns an SDL-owned string
                string? title = SDL_GetWindowTitle(window: _windowPtr);
                return title ?? string.Empty; // Return empty if null
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetWindowTitle(window: _windowPtr, title: value),
                    message: "Failed to set window title"
                );
            }
        }

        /// <summary>
        /// Gets or sets the position of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point Position
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetWindowPosition(window: _windowPtr, x: out int x, y: out int y),
                    message: "Failed to get window position"
                );
                return new Point(x, y);
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetWindowPosition(window: _windowPtr, x: value.X, y: value.Y),
                    message: "Failed to set window position"
                );
            }
        }

        /// <summary>
        /// Gets or sets the size of the window's client area.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point Size
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetWindowSize(window: _windowPtr, w: out int w, h: out int h),
                    message: "Failed to get window size"
                );
                return new Point(w, h);
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetWindowSize(window: _windowPtr, w: value.X, h: value.Y),
                    message: "Failed to set window size"
                );
            }
        }

        /// <summary>
        /// Gets the size of the window's client area in pixels (for high-DPI displays).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point SizeInPixels
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetWindowSizeInPixels(
                        window: _windowPtr,
                        w: out int w,
                        h: out int h
                    ),
                    message: "Failed to get window size in pixels"
                );
                return new Point(w, h);
            }
        }

        /// <summary>
        /// Gets the current window flags.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_WindowFlags Flags
        {
            get
            {
                return _disposed
                    ? throw new ObjectDisposedException(nameof(Window))
                    : SDL_GetWindowFlags(window: _windowPtr);
            }
        }

        /// <summary>
        /// Gets the display index associated with the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public uint DisplayId
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                // SDL_GetDisplayForWindow returns 0 on error, check error state?
                // SDL3 doc says returns a valid display ID or 0 if the window is invalid.
                // Assuming 0 is a valid ID for the primary display if only one exists,
                // or an error if the window handle is bad (which constructor should prevent).
                return SDL_GetDisplayForWindow(window: _windowPtr);
            }
        }

        // --- Window Methods ---

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Show()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_ShowWindow(window: _windowPtr),
                message: "Failed to show window"
            );
        }

        /// <summary>
        /// Hides the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Hide()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(
                result: SDL_HideWindow(window: _windowPtr),
                message: "Failed to hide window"
            );
        }

        /// <summary>
        /// Raises the window above other windows and requests input focus.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Raise()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RaiseWindow(window: _windowPtr),
                message: "Failed to raise window"
            );
        }

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Maximize()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(
                result: SDL_MaximizeWindow(window: _windowPtr),
                message: "Failed to maximize window"
            );
        }

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Minimize()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_MinimizeWindow(window: _windowPtr),
                message: "Failed to minimize window"
            );
        }

        /// <summary>
        /// Restores the size and position of a minimized or maximized window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Restore()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RestoreWindow(window: _windowPtr),
                message: "Failed to restore window"
            );
        }

        /// <summary>
        /// Sets the window to fullscreen mode.
        /// </summary>
        /// <param name="fullscreen">True for fullscreen, false otherwise.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetFullscreen(bool fullscreen)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetWindowFullscreen(window: _windowPtr, fullscreen: fullscreen),
                message: fullscreen ? "Failed to enter fullscreen" : "Failed to leave fullscreen"
            );
        }

        /// <summary>
        /// Sets the border state of the window.
        /// </summary>
        /// <param name="bordered">True to enable border, false to disable.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetBordered(bool bordered)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetWindowBordered(window: _windowPtr, bordered: bordered),
                message: "Failed to set window border state"
            );
        }

        /// <summary>
        /// Sets whether the window is resizable.
        /// </summary>
        /// <param name="resizable">True if the window should be resizable.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetResizable(bool resizable)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(
                result: SDL_SetWindowResizable(window: _windowPtr, resizable: resizable),
                message: "Failed to set window resizable state"
            );
        }

        /// <summary>
        /// Sets whether the window should always be on top.
        /// </summary>
        /// <param name="onTop">True if the window should be always on top.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetAlwaysOnTop(bool onTop)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(
                result: SDL_SetWindowAlwaysOnTop(window: _windowPtr, on_top: onTop),
                message: "Failed to set window always on top state"
            );
        }

        /// <summary>
        /// Creates a renderer associated with this window.
        /// </summary>
        /// <param name="driverName">Optional name of the rendering driver to use (e.g., "direct3d11", "opengl", "metal"). Null for default.</param>
        /// <returns>A new Renderer instance.</returns>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public Renderer CreateRenderer(string? driverName = null)
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(Window))
                : new Renderer(this, driverName);
        }

        /// <summary>
        /// Gets the window associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the window.</param>
        /// <returns>A Window object wrapping the native handle, or null if not found.</returns>
        /// <remarks>The returned Window object does NOT own the native handle and will not destroy it on Dispose.</remarks>
        public static Window? GetFromId(uint id)
        {
            IntPtr handle = SDL_GetWindowFromID(id: id);
            return handle == IntPtr.Zero ? null : new Window(handle);
        }

        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Only destroy the window if this object instance created it (owns it).
                // Handles obtained via GetWindowFromID are not owned.
                // We currently mark externally obtained handles as _disposed = true in the internal constructor.
                // A more robust solution might involve an '_owned' flag.
                const bool owned = true; // Assume ownership unless created with internal constructor

                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // None in this basic example.
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_windowPtr != IntPtr.Zero && owned)
                {
                    // Make sure SDL_DestroyWindow is safe to call even if SDL is shut down
                    // (SDL3 documentation should clarify this, assuming it is safe here)
                    SDL_DestroyWindow(window: _windowPtr);
                }
                _windowPtr = IntPtr.Zero; // Prevent use after dispose
                _disposed = true; // Mark as disposed regardless of ownership to prevent method calls
            }
        }

        /// <summary>
        /// Releases the resources used by the Window.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(obj: this);
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Window()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
    }

    #endregion

    #region Renderer and Texture

    /// <summary>
    /// Represents an SDL Texture, used for rendering images.
    /// </summary>
    public class Texture : IDisposable
    {
        private IntPtr _texturePtr;
        private bool _disposed = false;
        public bool IsDisposed
        {
            get => _disposed;
        }

        /// <summary>
        /// Gets the native SDL texture handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Texture)) : _texturePtr;
            }
        }

        /// <summary>
        /// Gets the renderer associated with this texture.
        /// </summary>
        public Renderer Renderer { get; }

        // Internal constructor for wrapping SDL_CreateTexture and SDL_CreateTextureFromSurface
        internal Texture(
            Renderer renderer,
            SDL_PixelFormat format,
            SDL_TextureAccess access,
            int w,
            int h
        )
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (renderer.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Renderer));
            }

            unsafe // SDL_CreateTexture returns SDL_Texture*
            {
                SDL_Texture* texPtr = SDL_CreateTexture(
                    renderer: renderer.Handle,
                    format: format,
                    access: access,
                    w: w,
                    h: h
                );
                SdlHost.ThrowOnNull(ptr: (IntPtr)texPtr, message: "Failed to create texture");
                _texturePtr = (IntPtr)texPtr;
            }
        }

        // Internal constructor for wrapping SDL_CreateTextureFromSurface
        // NOTE: Requires Surface class to be implemented. Placeholder for now.
        internal Texture(
            Renderer renderer, /* Surface surface */
            IntPtr surfacePtr
        )
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (renderer.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Renderer));
            }

            if (surfacePtr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(surfacePtr)); // Replace with Surface object check later
            }

            unsafe // SDL_CreateTextureFromSurface returns SDL_Texture*
            {
                SDL_Texture* texPtr = SDL_CreateTextureFromSurface(
                    renderer: renderer.Handle,
                    surface: surfacePtr
                );
                SdlHost.ThrowOnNull(
                    ptr: (IntPtr)texPtr,
                    message: "Failed to create texture from surface"
                );
                _texturePtr = (IntPtr)texPtr;
            }
            // SDL_CreateTextureFromSurface documentation implies the surface is no longer needed
            // after this call, but doesn't explicitly say it frees it.
            // If the Surface wrapper owns the surface, its Dispose should handle it.
        }

        // --- Texture Properties ---

        /// <summary>
        /// Gets the format of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_PixelFormat Format
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Texture));
                }
                // Query the format using properties (SDL3 style) or internal storage
                // For simplicity, assuming the format doesn't change after creation.
                // A more robust way would query SDL_GetTextureProperties.
                unsafe
                {
                    // SDL_Texture struct is defined in the bindings
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->format;
                    }
                    return SDL_PixelFormat.SDL_PIXELFORMAT_UNKNOWN; // Or throw?
                }
            }
        }

        /// <summary>
        /// Gets the access mode of the texture (static, streaming, target).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_TextureAccess Access
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                // Query access mode - SDL3 doesn't have a direct SDL_QueryTexture equivalent for access anymore?
                // Need to check SDL_GetTextureProperties. Let's assume it's stored or inferred.
                // Placeholder: Need a way to get this. Maybe store it during creation?
                // For now, returning a default or throwing.
                // Let's try getting it from the struct if available
                unsafe
                {
                    // SDL_Texture struct doesn't contain access mode directly.
                    // Need to use SDL_GetTextureProperties
                    uint props = SDL_GetTextureProperties(texture: _texturePtr);
                    SdlHost.ThrowOnFailure(
                        result: props == 0,
                        message: "Failed to get texture properties"
                    ); // Check if 0 is error

                    // We don't destroy the props handle here, assume it's temporary or managed by SDL? Check SDL docs.
                    return (SDL_TextureAccess)SDL_GetNumberProperty(
                        props: props,
                        name: SDL_PROP_TEXTURE_ACCESS_NUMBER,
                        default_value: (long)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC
                    );
                }
            }
        }

        /// <summary>
        /// Gets the width of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public int Width
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                unsafe
                {
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->w;
                    }

                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the height of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public int Height
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                unsafe
                {
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->h;
                    }

                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the size of the texture.
        /// </summary>
        public Point Size => new Point(Width, Height);

        // --- Texture Manipulation ---

        /// <summary>
        /// Sets the color modulation for this texture.
        /// </summary>
        /// <param name="color">The color to modulate with.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetColorMod(Color color)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetTextureColorMod(
                    texture: _texturePtr,
                    r: color.R,
                    g: color.G,
                    b: color.B
                ),
                message: "Failed to set texture color mod"
            );
        }

        /// <summary>
        /// Gets the color modulation for this texture.
        /// </summary>
        /// <returns>The current color modulation.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public Color GetColorMod()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_GetTextureColorMod(
                    texture: _texturePtr,
                    r: out byte r,
                    g: out byte g,
                    b: out byte b
                ),
                message: "Failed to get texture color mod"
            );
            return new Color(r, g, b);
        }

        /// <summary>
        /// Sets the alpha modulation for this texture.
        /// </summary>
        /// <param name="alpha">The alpha value (0-255).</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetAlphaMod(byte alpha)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetTextureAlphaMod(texture: _texturePtr, alpha: alpha),
                message: "Failed to set texture alpha mod"
            );
        }

        /// <summary>
        /// Gets the alpha modulation for this texture.
        /// </summary>
        /// <returns>The current alpha modulation value.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public byte GetAlphaMod()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_GetTextureAlphaMod(texture: _texturePtr, alpha: out byte alpha),
                message: "Failed to get texture alpha mod"
            );
            return alpha;
        }

        /// <summary>
        /// Sets the scale mode used for texture scaling operations.
        /// </summary>
        /// <param name="scaleMode">The scale mode to use.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetScaleMode(SDL_ScaleMode scaleMode)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetTextureScaleMode(texture: _texturePtr, scaleMode: scaleMode),
                message: "Failed to set texture scale mode"
            );
        }

        /// <summary>
        /// Gets the scale mode used for texture scaling operations.
        /// </summary>
        /// <returns>The current scale mode.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_ScaleMode GetScaleMode()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_GetTextureScaleMode(
                    texture: _texturePtr,
                    scaleMode: out SDL_ScaleMode scaleMode
                ),
                message: "Failed to get texture scale mode"
            );
            return scaleMode;
        }

        /// <summary>
        /// Updates a portion of the texture with new pixel data.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to update, or null for the entire texture.</param>
        /// <param name="pixels">A pointer to the pixel data.</param>
        /// <param name="pitch">The number of bytes per row in the pixel data.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void UpdateTexture(Rect? rect, IntPtr pixels, int pitch)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(
                result: SDL_UpdateTexture(
                    texture: _texturePtr,
                    rect: ref sdlRect,
                    pixels: pixels,
                    pitch: pitch
                ),
                message: "Failed to update texture"
            );
        }

        /// <summary>
        /// Updates a portion of a YUV planar texture with new pixel data.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to update, or null for the entire texture.</param>
        /// <param name="yPlane">Pointer to the Y plane data.</param>
        /// <param name="yPitch">Pitch of the Y plane data.</param>
        /// <param name="uPlane">Pointer to the U plane data.</param>
        /// <param name="uPitch">Pitch of the U plane data.</param>
        /// <param name="vPlane">Pointer to the V plane data.</param>
        /// <param name="vPitch">Pitch of the V plane data.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void UpdateYUVTexture(
            Rect? rect,
            IntPtr yPlane,
            int yPitch,
            IntPtr uPlane,
            int uPitch,
            IntPtr vPlane,
            int vPitch
        )
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(
                result: SDL_UpdateYUVTexture(
                    texture: _texturePtr,
                    rect: ref sdlRect,
                    Yplane: yPlane,
                    Ypitch: yPitch,
                    Uplane: uPlane,
                    Upitch: uPitch,
                    Vplane: vPlane,
                    Vpitch: vPitch
                ),
                message: "Failed to update YUV texture"
            );
        }

        /// <summary>
        /// Locks a portion of the texture for direct pixel access.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to lock, or null for the entire texture.</param>
        /// <param name="pixels">Outputs a pointer to the locked pixels.</param>
        /// <param name="pitch">Outputs the pitch (bytes per row) of the locked pixels.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Lock(Rect? rect, out IntPtr pixels, out int pitch)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(
                result: SDL_LockTexture(
                    texture: _texturePtr,
                    rect: ref sdlRect,
                    pixels: out pixels,
                    pitch: out pitch
                ),
                message: "Failed to lock texture"
            );
        }

        /// <summary>
        /// Unlocks a texture previously locked with LockTexture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Unlock()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            // SDL_UnlockTexture returns void, no error check needed unless documented otherwise
            SDL_UnlockTexture(texture: _texturePtr);
        }

        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_texturePtr != IntPtr.Zero)
                {
                    // Check if the renderer is still valid before destroying?
                    // SDL documentation usually implies Destroy functions are safe.
                    SDL_DestroyTexture(texture: _texturePtr);
                    _texturePtr = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the Texture.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(obj: this);
        }

        ~Texture()
        {
            Dispose(disposing: false);
        }
    }

    /// <summary>
    /// Represents an SDL Renderer, used for drawing operations.
    /// </summary>
    public class Renderer : IDisposable
    {
        private IntPtr _rendererPtr;
        private bool _disposed = false;

        /// <summary>
        /// Gets the native SDL renderer handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed
                    ? throw new ObjectDisposedException(nameof(Renderer))
                    : _rendererPtr;
            }
        }

        /// <summary>
        /// Gets whether the Renderer has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Creates a new renderer associated with a window.
        /// </summary>
        /// <param name="window">The window where rendering is displayed.</param>
        /// <param name="driverName">Optional name of the rendering driver (e.g., "direct3d11", "opengl"). Null for default.</param>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <exception cref="ArgumentNullException">Thrown if window is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the window is disposed.</exception>
        public Renderer(Window window, string? driverName = null)
        {
            AssociatedWindow = window ?? throw new ArgumentNullException(nameof(window));
            if (window.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            _rendererPtr = SDL_CreateRenderer(window: window.Handle, name: driverName);
            SdlHost.ThrowOnNull(
                ptr: _rendererPtr,
                message: $"Failed to create renderer{(driverName == null ? "" : $" with driver '{driverName}'")}"
            );
        }

        /// <summary>
        /// Creates a new renderer using properties.
        /// </summary>
        /// <param name="properties">The properties handle created via SDL_CreateProperties().</param>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <remarks>The caller is responsible for destroying the properties handle AFTER the renderer is created.</remarks>
        public Renderer(uint properties)
        {
            _rendererPtr = SDL_CreateRendererWithProperties(props: properties);
            SdlHost.ThrowOnNull(
                ptr: _rendererPtr,
                message: "Failed to create renderer with properties"
            );
            // Try to get the associated window from properties if possible
            IntPtr windowHandle = SDL_GetPointerProperty(
                props: properties,
                name: SDL_PROP_RENDERER_CREATE_WINDOW_POINTER,
                default_value: IntPtr.Zero
            );
            if (windowHandle != IntPtr.Zero)
            {
                AssociatedWindow = Window.GetFromId(id: SDL_GetWindowID(window: windowHandle));
                // Note: _associatedWindow might be null if GetFromId fails or returns a disposed window wrapper
            }
        }

        // TODO: Add constructor for software renderer SDL_CreateSoftwareRenderer(Surface surface) when Surface is wrapped.

        // --- Renderer Properties ---

        /// <summary>
        /// Gets the window associated with this renderer, if any.
        /// </summary>
        public Window? AssociatedWindow { get; }

        /// <summary>
        /// Gets the name of the rendering driver.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public string Name
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                string? name = SDL_GetRendererName(renderer: _rendererPtr);
                return name ?? string.Empty;
            }
        }

        private bool _vSync;

        /// <summary>
        /// Gets or sets a value indicating whether VSync is enabled for the renderer.
        /// When set, updates the internal state and the SDL renderer's VSync setting.
        /// </summary>
        public bool VSync
        {
            get => _vSync;
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);
                if (_vSync != value)
                {
                    _vSync = value;
                    SdlHost.ThrowOnFailure(
                        result: SDL_SetRenderVSync(renderer: _rendererPtr, vsync: value ? 1 : 0),
                        message: "Failed to set window always on top state"
                    );
                }
            }
        }

        /// <summary>
        /// Gets the output size in pixels of the rendering context.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point OutputSize
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderOutputSize(
                        renderer: _rendererPtr,
                        w: out int w,
                        h: out int h
                    ),
                    message: "Failed to get render output size"
                );
                return new Point(w, h);
            }
        }

        /// <summary>
        /// Gets or sets the drawing color for the renderer.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Color DrawColor
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderDrawColor(
                        renderer: _rendererPtr,
                        r: out byte r,
                        g: out byte g,
                        b: out byte b,
                        a: out byte a
                    ),
                    message: "Failed to get draw color"
                );
                return new Color(r, g, b, a);
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetRenderDrawColor(
                        renderer: _rendererPtr,
                        r: value.R,
                        g: value.G,
                        b: value.B,
                        a: value.A
                    ),
                    message: "Failed to set draw color"
                );
            }
        }

        /// <summary>
        /// Gets or sets the drawing color for the renderer using float values (0.0f to 1.0f).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public FColor DrawColorF
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderDrawColorFloat(
                        renderer: _rendererPtr,
                        r: out float r,
                        g: out float g,
                        b: out float b,
                        a: out float a
                    ),
                    message: "Failed to get draw color float"
                );
                return new FColor(r, g, b, a);
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetRenderDrawColorFloat(
                        renderer: _rendererPtr,
                        r: value.R,
                        g: value.G,
                        b: value.B,
                        a: value.A
                    ),
                    message: "Failed to set draw color float"
                );
            }
        }

        /// <summary>
        /// Gets or sets the logical presentation mode for rendering when the aspect ratio of the window and logical size differ.
        /// </summary>
        /// <remarks>Requires setting the logical size first.</remarks>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_RendererLogicalPresentation LogicalPresentation
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderLogicalPresentation(
                        renderer: _rendererPtr,
                        w: out _,
                        h: out _,
                        mode: out SDL_RendererLogicalPresentation mode
                    ),
                    message: "Failed to get logical presentation"
                );
                return mode;
            }
            // Setting requires width/height, so use the SetRenderLogicalPresentation method instead.
        }

        /// <summary>
        /// Gets or sets the drawing scale for the renderer.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public FPoint Scale
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderScale(
                        renderer: _rendererPtr,
                        scaleX: out float x,
                        scaleY: out float y
                    ),
                    message: "Failed to get render scale"
                );
                return new FPoint(x, y);
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_SetRenderScale(
                        renderer: _rendererPtr,
                        scaleX: value.X,
                        scaleY: value.Y
                    ),
                    message: "Failed to set render scale"
                );
            }
        }

        /// <summary>
        /// Gets or sets the drawing area for rendering on the current target.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Rect Viewport
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderViewport(renderer: _rendererPtr, rect: out SDL_Rect rect),
                    message: "Failed to get viewport"
                );
                return rect; // Implicit conversion
            }
            set
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                SDL_Rect rect = value; // Implicit conversion
                SdlHost.ThrowOnFailure(
                    result: SDL_SetRenderViewport(renderer: _rendererPtr, rect: ref rect),
                    message: "Failed to set viewport"
                );
            }
        }

        /// <summary>
        /// Gets or sets the clipping rectangle for the current target.
        /// </summary>
        /// <remarks>Set to null to disable clipping.</remarks>
        /// <exception cref="ObjectDisposedException"></exception>
        public Rect? ClipRect
        {
            get
            {
                ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

                if (!SDL_RenderClipEnabled(renderer: _rendererPtr))
                {
                    return null;
                }

                SdlHost.ThrowOnFailure(
                    result: SDL_GetRenderClipRect(renderer: _rendererPtr, rect: out SDL_Rect rect),
                    message: "Failed to get clip rect"
                );
                return rect;
            }
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                SDL_Rect rect = value ?? default;
                SdlHost.ThrowOnFailure(
                    result: SDL_SetRenderClipRect(renderer: _rendererPtr, rect: ref rect),
                    message: "Failed to set clip rect"
                );
            }
        }

        /// <summary>
        /// Gets a value indicating whether clipping is enabled for the current target.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool ClipEnabled
        {
            get
            {
                return _disposed
                    ? throw new ObjectDisposedException(nameof(Renderer))
                    : SDL_RenderClipEnabled(renderer: _rendererPtr);
            }
        }

        // --- Renderer Methods ---

        /// <summary>
        /// Clears the current rendering target with the drawing color.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Clear()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RenderClear(renderer: _rendererPtr),
                message: "Failed to clear renderer"
            );
        }

        /// <summary>
        /// Updates the screen with any rendering performed since the previous call.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Present()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RenderPresent(renderer: _rendererPtr),
                message: "Failed to present renderer"
            );
        }

        /// <summary>
        /// Force the rendering context to flush any pending commands.
        /// You do not need to (and in fact, shouldn't) call this function unless
        /// you are planning to call into the underlying graphics API directly.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Flush()
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_FlushRenderer(renderer: _rendererPtr),
                message: "Failed to flush renderer"
            );
        }

        /// <summary>
        /// Draws a point on the current rendering target.
        /// </summary>
        /// <param name="point">The point to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawPoint(FPoint point)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RenderPoint(renderer: _rendererPtr, x: point.X, y: point.Y),
                message: "Failed to draw point"
            );
        }

        /// <summary>
        /// Draws multiple points on the current rendering target.
        /// </summary>
        /// <param name="points">The points to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawPoints(ReadOnlySpan<FPoint> points)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            if (points.IsEmpty)
            {
                return;
            }
            // SDL_RenderPoints expects SDL_FPoint[], need to convert or use unsafe context if bindings don't handle Span directly.
            // Assuming the binding `Span<SDL_FPoint>` works. Need to convert FPoint to SDL_FPoint.
            // This is inefficient. Consider providing an overload accepting SDL_FPoint or using unsafe code.
            var sdlPoints = new SDL_FPoint[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                sdlPoints[i] = points[i];
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RenderPoints(
                    renderer: _rendererPtr,
                    points: sdlPoints,
                    count: points.Length
                ),
                message: "Failed to draw points"
            );
        }

        /// <summary>
        /// Draws a line on the current rendering target.
        /// </summary>
        /// <param name="p1">The start point.</param>
        /// <param name="p2">The end point.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawLine(FPoint p1, FPoint p2)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RenderLine(
                    renderer: _rendererPtr,
                    x1: p1.X,
                    y1: p1.Y,
                    x2: p2.X,
                    y2: p2.Y
                ),
                message: "Failed to draw line"
            );
        }

        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_RenderLine(renderer: _rendererPtr, x1: x1, y1: y1, x2: x2, y2: y2),
                message: "Failed to draw line"
            );
        }

        /// <summary>
        /// Draws a sequence of connected lines on the current rendering target.
        /// </summary>
        /// <param name="points">The points defining the lines. A line is drawn between points[i] and points[i+1].</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawLines(ReadOnlySpan<FPoint> points)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            if (points.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlPoints = new SDL_FPoint[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                sdlPoints[i] = points[i];
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RenderLines(
                    renderer: _rendererPtr,
                    points: sdlPoints,
                    count: points.Length
                ),
                message: "Failed to draw lines"
            );
        }

        /// <summary>
        /// Draws the outline of a rectangle on the current rendering target.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawRect(FRect rect)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SDL_FRect sdlRect = rect; // Implicit conversion
            SdlHost.ThrowOnFailure(
                result: SDL_RenderRect(renderer: _rendererPtr, rect: ref sdlRect),
                message: "Failed to draw rect"
            );
        }

        /// <summary>
        /// Draws the outlines of multiple rectangles on the current rendering target.
        /// </summary>
        /// <param name="rects">The rectangles to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawRects(ReadOnlySpan<FRect> rects)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            if (rects.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlRects = new SDL_FRect[rects.Length];
            for (int i = 0; i < rects.Length; ++i)
            {
                sdlRects[i] = rects[i];
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RenderRects(
                    renderer: _rendererPtr,
                    rects: sdlRects,
                    count: rects.Length
                ),
                message: "Failed to draw rects"
            );
        }

        /// <summary>
        /// Fills a rectangle on the current rendering target with the drawing color.
        /// </summary>
        /// <param name="rect">The rectangle to fill.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void FillRect(FRect rect)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SDL_FRect sdlRect = rect; // Implicit conversion
            SdlHost.ThrowOnFailure(
                result: SDL_RenderFillRect(renderer: _rendererPtr, rect: ref sdlRect),
                message: "Failed to fill rect"
            );
        }

        /// <summary>
        /// Fills multiple rectangles on the current rendering target with the drawing color.
        /// </summary>
        /// <param name="rects">The rectangles to fill.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void FillRects(ReadOnlySpan<FRect> rects)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            if (rects.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlRects = new SDL_FRect[rects.Length];
            for (int i = 0; i < rects.Length; ++i)
            {
                sdlRects[i] = rects[i];
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RenderFillRects(
                    renderer: _rendererPtr,
                    rects: sdlRects,
                    count: rects.Length
                ),
                message: "Failed to fill rects"
            );
        }

        /// <summary>
        /// Copies a portion of a texture to the current rendering target.
        /// </summary>
        /// <param name="texture">The source texture.</param>
        /// <param name="srcRect">The source rectangle, or null for the entire texture.</param>
        /// <param name="dstRect">The destination rectangle, or null for the entire rendering target.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <c>null</c>.</exception>
        public void Copy(Texture texture, FRect? srcRect, FRect? dstRect)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            ArgumentNullException.ThrowIfNull(argument: texture);

            ObjectDisposedException.ThrowIf(condition: texture.IsDisposed, instance: this);

            SDL_FRect sdlSrcRect = srcRect ?? default;
            SDL_FRect sdlDstRect = dstRect ?? default;

            // SDL_RenderTexture takes pointers, need to handle nullability
            unsafe
            {
                SDL_FRect* pSrc = srcRect.HasValue ? &sdlSrcRect : null;
                SDL_FRect* pDst = dstRect.HasValue ? &sdlDstRect : null;
                // Need to check the binding: SDL_RenderTexture takes pointers or refs?
                // The binding shows `ref SDL_FRect srcrect, ref SDL_FRect dstrect`. This is problematic for null.
                // Let's assume the binding should have used pointers or we need an overload.
                // HACK: Using default rects if null, assuming SDL handles {0,0,0,0} correctly for "entire texture/target".
                // This might not be correct SDL behavior. A binding fix or overload is better.
                if (!srcRect.HasValue)
                {
                    sdlSrcRect = default;
                }

                if (!dstRect.HasValue)
                {
                    sdlDstRect = default;
                }

                SdlHost.ThrowOnFailure(
                    result: SDL_RenderTexture(
                        renderer: _rendererPtr,
                        texture: texture.Handle,
                        srcrect: ref sdlSrcRect,
                        dstrect: ref sdlDstRect
                    ),
                    message: "Failed to copy texture"
                );
            }
        }

        /// <summary>
        /// Copies a portion of a texture to the current rendering target, rotating it and flipping it.
        /// </summary>
        /// <param name="texture">The source texture.</param>
        /// <param name="srcRect">The source rectangle, or null for the entire texture.</param>
        /// <param name="dstRect">The destination rectangle, or null for the entire rendering target.</param>
        /// <param name="angle">An angle in degrees for rotation.</param>
        /// <param name="center">The point to rotate around, or null for the center of dstRect.</param>
        /// <param name="flip">Flip mode.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <c>null</c>.</exception>
        public void CopyEx(
            Texture texture,
            FRect? srcRect,
            FRect? dstRect,
            double angle,
            FPoint? center,
            SDL_FlipMode flip
        )
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);
            ArgumentNullException.ThrowIfNull(argument: texture);

            SDL_FRect sdlSrcRect = srcRect ?? default;
            SDL_FRect sdlDstRect = dstRect ?? default;
            SDL_FPoint sdlCenter = center ?? default; // Default might not be correct center

            // HACK: Handle null pointers similar to Copy method.
            if (!srcRect.HasValue)
            {
                sdlSrcRect = default;
            }

            if (!dstRect.HasValue)
            {
                sdlDstRect = default;
            }

            if (!center.HasValue && dstRect.HasValue)
            {
                // Calculate center if not provided
                sdlCenter = new SDL_FPoint { x = sdlDstRect.w / 2.0f, y = sdlDstRect.h / 2.0f };
            }
            else if (!center.HasValue)
            {
                // Cannot calculate center if dstRect is also null
                sdlCenter = default; // Or throw?
            }

            SdlHost.ThrowOnFailure(
                result: SDL_RenderTextureRotated(
                    renderer: _rendererPtr,
                    texture: texture.Handle,
                    srcrect: ref sdlSrcRect,
                    dstrect: ref sdlDstRect,
                    angle: angle,
                    center: ref sdlCenter,
                    flip: flip
                ),
                message: "Failed to copy texture (ex)"
            );
        }

        /// <summary>
        /// Renders geometry defined by vertices and optional indices.
        /// </summary>
        /// <param name="texture">The texture to apply to the geometry, or null for untextured.</param>
        /// <param name="vertices">The vertices defining the geometry.</param>
        /// <param name="indices">The indices mapping vertices to triangles, or null if vertices are already ordered triangles.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void RenderGeometry(
            Texture? texture,
            ReadOnlySpan<Vertex> vertices,
            ReadOnlySpan<int> indices = default
        )
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            if (vertices.IsEmpty)
            {
                return;
            }

            IntPtr textureHandle = texture != null ? texture.Handle : IntPtr.Zero;

            // Convert Vertex[] to SDL_Vertex[] - Inefficient, consider unsafe or direct SDL_Vertex usage
            var sdlVertices = new SDL_Vertex[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                sdlVertices[i] = vertices[i];
            }

            // Check if indices are provided
            int[]? indicesArray = indices.IsEmpty ? null : indices.ToArray(); // Convert Span to array if needed by binding

            SdlHost.ThrowOnFailure(
                result: SDL_RenderGeometry(
                    renderer: _rendererPtr,
                    texture: textureHandle,
                    vertices: sdlVertices,
                    num_vertices: vertices.Length,
                    indices: indicesArray,
                    num_indices: indices.Length
                ),
                message: "Failed to render geometry"
            );
        }

        /// <summary>
        /// Sets the logical size for rendering.
        /// </summary>
        /// <param name="w">The logical width.</param>
        /// <param name="h">The logical height.</param>
        /// <param name="mode">The presentation mode for scaling.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetLogicalPresentation(int w, int h, SDL_RendererLogicalPresentation mode)
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_SetRenderLogicalPresentation(
                    renderer: _rendererPtr,
                    w: w,
                    h: h,
                    mode: mode
                ),
                message: "Failed to set logical presentation"
            );
        }

        /// <summary>
        /// Gets the logical size for rendering.
        /// </summary>
        /// <param name="w">Outputs the logical width.</param>
        /// <param name="h">Outputs the logical height.</param>
        /// <param name="mode">Outputs the presentation mode.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void GetLogicalPresentation(
            out int w,
            out int h,
            out SDL_RendererLogicalPresentation mode
        )
        {
            ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);

            SdlHost.ThrowOnFailure(
                result: SDL_GetRenderLogicalPresentation(
                    renderer: _rendererPtr,
                    w: out w,
                    h: out h,
                    mode: out mode
                ),
                message: "Failed to get logical presentation"
            );
        }

        /// <summary>
        /// Creates a texture for rendering.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="access">The texture access mode.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>A new Texture instance.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public Texture CreateTexture(
            SDL_PixelFormat format,
            SDL_TextureAccess access,
            int width,
            int height
        )
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(Renderer))
                : new Texture(this, format, access, width, height);
        }

        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // e.g., if we cached Texture objects created by GetRenderTarget
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_rendererPtr != IntPtr.Zero)
                {
                    SDL_DestroyRenderer(renderer: _rendererPtr);
                    _rendererPtr = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the Renderer.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(obj: this);
        }

        ~Renderer()
        {
            Dispose(disposing: false);
        }
    }

    #endregion

    #region Input Abstraction (Keyboard)

    /// <summary>
    /// Represents physical key locations on a keyboard, independent of layout.
    /// Maps closely to SDL_Scancode.
    /// </summary>
    public enum Key
    {
        Unknown = SDL_Scancode.SDL_SCANCODE_UNKNOWN,

        A = SDL_Scancode.SDL_SCANCODE_A,
        B = SDL_Scancode.SDL_SCANCODE_B,
        C = SDL_Scancode.SDL_SCANCODE_C,
        D = SDL_Scancode.SDL_SCANCODE_D,
        E = SDL_Scancode.SDL_SCANCODE_E,
        F = SDL_Scancode.SDL_SCANCODE_F,
        G = SDL_Scancode.SDL_SCANCODE_G,
        H = SDL_Scancode.SDL_SCANCODE_H,
        I = SDL_Scancode.SDL_SCANCODE_I,
        J = SDL_Scancode.SDL_SCANCODE_J,
        K = SDL_Scancode.SDL_SCANCODE_K,
        L = SDL_Scancode.SDL_SCANCODE_L,
        M = SDL_Scancode.SDL_SCANCODE_M,
        N = SDL_Scancode.SDL_SCANCODE_N,
        O = SDL_Scancode.SDL_SCANCODE_O,
        P = SDL_Scancode.SDL_SCANCODE_P,
        Q = SDL_Scancode.SDL_SCANCODE_Q,
        R = SDL_Scancode.SDL_SCANCODE_R,
        S = SDL_Scancode.SDL_SCANCODE_S,
        T = SDL_Scancode.SDL_SCANCODE_T,
        U = SDL_Scancode.SDL_SCANCODE_U,
        V = SDL_Scancode.SDL_SCANCODE_V,
        W = SDL_Scancode.SDL_SCANCODE_W,
        X = SDL_Scancode.SDL_SCANCODE_X,
        Y = SDL_Scancode.SDL_SCANCODE_Y,
        Z = SDL_Scancode.SDL_SCANCODE_Z,

        D1 = SDL_Scancode.SDL_SCANCODE_1,
        D2 = SDL_Scancode.SDL_SCANCODE_2,
        D3 = SDL_Scancode.SDL_SCANCODE_3,
        D4 = SDL_Scancode.SDL_SCANCODE_4,
        D5 = SDL_Scancode.SDL_SCANCODE_5,
        D6 = SDL_Scancode.SDL_SCANCODE_6,
        D7 = SDL_Scancode.SDL_SCANCODE_7,
        D8 = SDL_Scancode.SDL_SCANCODE_8,
        D9 = SDL_Scancode.SDL_SCANCODE_9,
        D0 = SDL_Scancode.SDL_SCANCODE_0,

        Return = SDL_Scancode.SDL_SCANCODE_RETURN,
        Escape = SDL_Scancode.SDL_SCANCODE_ESCAPE,
        Backspace = SDL_Scancode.SDL_SCANCODE_BACKSPACE,
        Tab = SDL_Scancode.SDL_SCANCODE_TAB,
        Space = SDL_Scancode.SDL_SCANCODE_SPACE,

        Minus = SDL_Scancode.SDL_SCANCODE_MINUS,
        Equals = SDL_Scancode.SDL_SCANCODE_EQUALS,
        LeftBracket = SDL_Scancode.SDL_SCANCODE_LEFTBRACKET,
        RightBracket = SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET,
        Backslash = SDL_Scancode.SDL_SCANCODE_BACKSLASH,
        Semicolon = SDL_Scancode.SDL_SCANCODE_SEMICOLON,
        Apostrophe = SDL_Scancode.SDL_SCANCODE_APOSTROPHE,
        Grave = SDL_Scancode.SDL_SCANCODE_GRAVE, // Backtick `
        Comma = SDL_Scancode.SDL_SCANCODE_COMMA,
        Period = SDL_Scancode.SDL_SCANCODE_PERIOD,
        Slash = SDL_Scancode.SDL_SCANCODE_SLASH,

        CapsLock = SDL_Scancode.SDL_SCANCODE_CAPSLOCK,

        F1 = SDL_Scancode.SDL_SCANCODE_F1,
        F2 = SDL_Scancode.SDL_SCANCODE_F2,
        F3 = SDL_Scancode.SDL_SCANCODE_F3,
        F4 = SDL_Scancode.SDL_SCANCODE_F4,
        F5 = SDL_Scancode.SDL_SCANCODE_F5,
        F6 = SDL_Scancode.SDL_SCANCODE_F6,
        F7 = SDL_Scancode.SDL_SCANCODE_F7,
        F8 = SDL_Scancode.SDL_SCANCODE_F8,
        F9 = SDL_Scancode.SDL_SCANCODE_F9,
        F10 = SDL_Scancode.SDL_SCANCODE_F10,
        F11 = SDL_Scancode.SDL_SCANCODE_F11,
        F12 = SDL_Scancode.SDL_SCANCODE_F12,

        PrintScreen = SDL_Scancode.SDL_SCANCODE_PRINTSCREEN,
        ScrollLock = SDL_Scancode.SDL_SCANCODE_SCROLLLOCK,
        Pause = SDL_Scancode.SDL_SCANCODE_PAUSE,
        Insert = SDL_Scancode.SDL_SCANCODE_INSERT,
        Home = SDL_Scancode.SDL_SCANCODE_HOME,
        PageUp = SDL_Scancode.SDL_SCANCODE_PAGEUP,
        Delete = SDL_Scancode.SDL_SCANCODE_DELETE,
        End = SDL_Scancode.SDL_SCANCODE_END,
        PageDown = SDL_Scancode.SDL_SCANCODE_PAGEDOWN,
        Right = SDL_Scancode.SDL_SCANCODE_RIGHT,
        Left = SDL_Scancode.SDL_SCANCODE_LEFT,
        Down = SDL_Scancode.SDL_SCANCODE_DOWN,
        Up = SDL_Scancode.SDL_SCANCODE_UP,

        NumLockClear = SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR,
        KeypadDivide = SDL_Scancode.SDL_SCANCODE_KP_DIVIDE,
        KeypadMultiply = SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY,
        KeypadMinus = SDL_Scancode.SDL_SCANCODE_KP_MINUS,
        KeypadPlus = SDL_Scancode.SDL_SCANCODE_KP_PLUS,
        KeypadEnter = SDL_Scancode.SDL_SCANCODE_KP_ENTER,
        Keypad1 = SDL_Scancode.SDL_SCANCODE_KP_1,
        Keypad2 = SDL_Scancode.SDL_SCANCODE_KP_2,
        Keypad3 = SDL_Scancode.SDL_SCANCODE_KP_3,
        Keypad4 = SDL_Scancode.SDL_SCANCODE_KP_4,
        Keypad5 = SDL_Scancode.SDL_SCANCODE_KP_5,
        Keypad6 = SDL_Scancode.SDL_SCANCODE_KP_6,
        Keypad7 = SDL_Scancode.SDL_SCANCODE_KP_7,
        Keypad8 = SDL_Scancode.SDL_SCANCODE_KP_8,
        Keypad9 = SDL_Scancode.SDL_SCANCODE_KP_9,
        Keypad0 = SDL_Scancode.SDL_SCANCODE_KP_0,
        KeypadPeriod = SDL_Scancode.SDL_SCANCODE_KP_PERIOD,

        Application = SDL_Scancode.SDL_SCANCODE_APPLICATION, // Menu key
        Power = SDL_Scancode.SDL_SCANCODE_POWER,
        KeypadEquals = SDL_Scancode.SDL_SCANCODE_KP_EQUALS,
        F13 = SDL_Scancode.SDL_SCANCODE_F13,
        F14 = SDL_Scancode.SDL_SCANCODE_F14,
        F15 = SDL_Scancode.SDL_SCANCODE_F15,
        F16 = SDL_Scancode.SDL_SCANCODE_F16,
        F17 = SDL_Scancode.SDL_SCANCODE_F17,
        F18 = SDL_Scancode.SDL_SCANCODE_F18,
        F19 = SDL_Scancode.SDL_SCANCODE_F19,
        F20 = SDL_Scancode.SDL_SCANCODE_F20,
        F21 = SDL_Scancode.SDL_SCANCODE_F21,
        F22 = SDL_Scancode.SDL_SCANCODE_F22,
        F23 = SDL_Scancode.SDL_SCANCODE_F23,
        F24 = SDL_Scancode.SDL_SCANCODE_F24,

        Execute = SDL_Scancode.SDL_SCANCODE_EXECUTE,
        Help = SDL_Scancode.SDL_SCANCODE_HELP,
        Menu = SDL_Scancode.SDL_SCANCODE_MENU,
        Select = SDL_Scancode.SDL_SCANCODE_SELECT,
        Stop = SDL_Scancode.SDL_SCANCODE_STOP,
        Again = SDL_Scancode.SDL_SCANCODE_AGAIN,
        Undo = SDL_Scancode.SDL_SCANCODE_UNDO,
        Cut = SDL_Scancode.SDL_SCANCODE_CUT,
        Copy = SDL_Scancode.SDL_SCANCODE_COPY,
        Paste = SDL_Scancode.SDL_SCANCODE_PASTE,
        Find = SDL_Scancode.SDL_SCANCODE_FIND,
        Mute = SDL_Scancode.SDL_SCANCODE_MUTE,
        VolumeUp = SDL_Scancode.SDL_SCANCODE_VOLUMEUP,
        VolumeDown = SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN,

        LeftCtrl = SDL_Scancode.SDL_SCANCODE_LCTRL,
        LeftShift = SDL_Scancode.SDL_SCANCODE_LSHIFT,
        LeftAlt = SDL_Scancode.SDL_SCANCODE_LALT, // Alt, Option
        LeftGui = SDL_Scancode.SDL_SCANCODE_LGUI, // Windows, Command, Meta
        RightCtrl = SDL_Scancode.SDL_SCANCODE_RCTRL,
        RightShift = SDL_Scancode.SDL_SCANCODE_RSHIFT,
        RightAlt = SDL_Scancode.SDL_SCANCODE_RALT, // Alt Gr, Option
        RightGui = SDL_Scancode.SDL_SCANCODE_RGUI, // Windows, Command, Meta

        // Add other keys as needed...
        Mode = SDL_Scancode.SDL_SCANCODE_MODE, // AltGr, Mode switch

        // Media keys
        MediaPlay = SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY,
        MediaPause = SDL_Scancode.SDL_SCANCODE_MEDIA_PAUSE,
        MediaRecord = SDL_Scancode.SDL_SCANCODE_MEDIA_RECORD,
        MediaFastForward = SDL_Scancode.SDL_SCANCODE_MEDIA_FAST_FORWARD,
        MediaRewind = SDL_Scancode.SDL_SCANCODE_MEDIA_REWIND,
        MediaNextTrack = SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK,
        MediaPreviousTrack = SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK,
        MediaStop = SDL_Scancode.SDL_SCANCODE_MEDIA_STOP,
        MediaEject = SDL_Scancode.SDL_SCANCODE_MEDIA_EJECT,
        MediaPlayPause = SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY_PAUSE,
        MediaSelect = SDL_Scancode.SDL_SCANCODE_MEDIA_SELECT,

        // Application control keys
        AppNew = SDL_Scancode.SDL_SCANCODE_AC_NEW,
        AppOpen = SDL_Scancode.SDL_SCANCODE_AC_OPEN,
        AppClose = SDL_Scancode.SDL_SCANCODE_AC_CLOSE,
        AppExit = SDL_Scancode.SDL_SCANCODE_AC_EXIT,
        AppSave = SDL_Scancode.SDL_SCANCODE_AC_SAVE,
        AppPrint = SDL_Scancode.SDL_SCANCODE_AC_PRINT,
        AppProperties = SDL_Scancode.SDL_SCANCODE_AC_PROPERTIES,
        AppSearch = SDL_Scancode.SDL_SCANCODE_AC_SEARCH,
        AppHome = SDL_Scancode.SDL_SCANCODE_AC_HOME,
        AppBack = SDL_Scancode.SDL_SCANCODE_AC_BACK,
        AppForward = SDL_Scancode.SDL_SCANCODE_AC_FORWARD,
        AppStop = SDL_Scancode.SDL_SCANCODE_AC_STOP,
        AppRefresh = SDL_Scancode.SDL_SCANCODE_AC_REFRESH,
        AppBookmarks = SDL_Scancode.SDL_SCANCODE_AC_BOOKMARKS,

        // Mobile keys
        SoftLeft = SDL_Scancode.SDL_SCANCODE_SOFTLEFT,
        SoftRight = SDL_Scancode.SDL_SCANCODE_SOFTRIGHT,
        Call = SDL_Scancode.SDL_SCANCODE_CALL,
        EndCall = SDL_Scancode.SDL_SCANCODE_ENDCALL,

        // Total number of scancodes. Not a key itself.
        Count = SDL_Scancode.SDL_SCANCODE_COUNT,
    }

    /// <summary>
    /// Represents keyboard modifier keys (Shift, Ctrl, Alt, Gui/Meta).
    /// Maps directly to SDL_Keymod flags.
    /// </summary>
    [Flags]
    public enum KeyModifier : ushort
    {
        None = SDL_Keymod.SDL_KMOD_NONE,
        LeftShift = SDL_Keymod.SDL_KMOD_LSHIFT,
        RightShift = SDL_Keymod.SDL_KMOD_RSHIFT,
        Shift = SDL_Keymod.SDL_KMOD_SHIFT,
        LeftCtrl = SDL_Keymod.SDL_KMOD_LCTRL,
        RightCtrl = SDL_Keymod.SDL_KMOD_RCTRL,

        // Combined flags for convenience
        Ctrl = SDL_Keymod.SDL_KMOD_CTRL,
        LeftAlt = SDL_Keymod.SDL_KMOD_LALT,
        RightAlt = SDL_Keymod.SDL_KMOD_RALT,
        Alt = SDL_Keymod.SDL_KMOD_ALT,
        LeftGui = SDL_Keymod.SDL_KMOD_LGUI, // Windows/Command/Meta key
        RightGui = SDL_Keymod.SDL_KMOD_RGUI, // Windows/Command/Meta key
        Gui = SDL_Keymod.SDL_KMOD_GUI,
        NumLock = SDL_Keymod.SDL_KMOD_NUM,
        CapsLock = SDL_Keymod.SDL_KMOD_CAPS,
        Mode = SDL_Keymod.SDL_KMOD_MODE, // AltGr
        ScrollLock = SDL_Keymod.SDL_KMOD_SCROLL,
    }

    /// <summary>
    /// Provides static methods for querying keyboard state.
    /// </summary>
    public static class Keyboard
    {
        private static readonly SDLBool[] _keyStates = new SDLBool[
            (int)SDL_Scancode.SDL_SCANCODE_COUNT
        ];
        private static readonly object _stateLock = new object(); // Lock for accessing shared state buffer

        // Static constructor to ensure the mapping is built once.
        static Keyboard()
        {
            // Pre-build mapping if needed, or do it on demand.
            // For now, mapping is direct via enum values.
        }

        /// <summary>
        /// Updates the internal keyboard state snapshot. Call this once per frame before checking keys.
        /// </summary>
        /// <remarks>
        /// This is optional if SDL_PumpEvents is called elsewhere, as SDL_GetKeyboardState
        /// uses an internal state updated by the event pump. However, calling this explicitly
        /// ensures the state used by IsKeyDown/IsKeyUp is consistent for the frame.
        /// </remarks>
        public static void UpdateState()
        {
            if (
                !SdlHost.IsInitialized
                || (SdlHost.WasInit(flags: SdlSubSystem.Events) & SdlSubSystem.Events) == 0
            )
            {
                throw new InvalidOperationException("SDL Events subsystem not initialized.");
            }

            // Get the current state array from SDL
            unsafe
            {
                // SDL_GetKeyboardState returns a pointer to an internal SDL state array.
                // We need to copy its contents safely.
                IntPtr statePtr = SDL_GetKeyboardState(numkeys: out int numkeys);
                if (statePtr == IntPtr.Zero)
                {
                    // This should not happen if EVENTS is initialized
                    throw new SDLException("SDL_GetKeyboardState returned NULL.");
                }

                // Ensure our internal buffer is large enough (should match SDL_SCANCODE_COUNT)
                if (numkeys > _keyStates.Length)
                {
                    // This indicates a mismatch between our enum and SDL's internal count. Problem!
                    throw new InvalidOperationException(
                        $"SDL returned {numkeys} keys, expected <= {(int)Key.Count}."
                    );
                }

                // Copy the state using Span for safety and efficiency
                lock (_stateLock)
                {
                    var sourceSpan = new ReadOnlySpan<SDLBool>((void*)statePtr, numkeys);
                    var destSpan = new Span<SDLBool>(_keyStates, 0, numkeys);
                    sourceSpan.CopyTo(destination: destSpan);

                    // Zero out remaining elements if SDL returns fewer keys than our enum size (unlikely but possible)
                    if (numkeys < _keyStates.Length)
                    {
                        Array.Clear(
                            array: _keyStates,
                            index: numkeys,
                            length: _keyStates.Length - numkeys
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a specific physical key is currently held down.
        /// Requires SDL_PumpEvents() or UpdateState() to have been called recently.
        /// </summary>
        /// <param name="key">The physical key to check.</param>
        /// <returns>True if the key is down, false otherwise.</returns>
        public static bool IsKeyDown(Key key)
        {
            if (key < 0 || (int)key >= _keyStates.Length)
                return false; // Bounds check

            lock (_stateLock)
            {
                // Access the pre-copied state
                // SDLBool implicitly converts to bool (true if non-zero/pressed)
                return _keyStates[(int)key];
            }
        }

        /// <summary>
        /// Checks if a specific physical key is currently released.
        /// Requires SDL_PumpEvents() or UpdateState() to have been called recently.
        /// </summary>
        /// <param name="key">The physical key to check.</param>
        /// <returns>True if the key is up, false otherwise.</returns>
        public static bool IsKeyUp(Key key)
        {
            return !IsKeyDown(key: key);
        }

        /// <summary>
        /// Gets the current state of the modifier keys (Shift, Ctrl, Alt, Gui, CapsLock, NumLock, etc.).
        /// </summary>
        /// <returns>A KeyModifier flags enum representing the active modifiers.</returns>
        public static KeyModifier GetModifiers()
        {
            if (
                !SdlHost.IsInitialized
                || (SdlHost.WasInit(flags: SdlSubSystem.Events) & SdlSubSystem.Events) == 0
            )
            {
                throw new InvalidOperationException("SDL Events subsystem not initialized.");
            }
            // SDL_GetModState returns the SDL_Keymod flags directly
            return (KeyModifier)SDL_GetModState();
        }

        /// <summary>
        /// Gets the wrapper Key enum value corresponding to an SDL Scancode.
        /// </summary>
        /// <param name="scancode">The SDL Scancode.</param>
        /// <returns>The corresponding Key enum value.</returns>
        public static Key GetKeyFromScancode(SDL_Scancode scancode)
        {
            // Direct cast is possible because we defined Key enum values based on SDL_Scancode
            return (Key)scancode;
        }

        /// <summary>
        /// Gets the SDL Scancode corresponding to a wrapper Key enum value.
        /// </summary>
        /// <param name="key">The wrapper Key enum value.</param>
        /// <returns>The corresponding SDL Scancode.</returns>
        public static SDL_Scancode GetScancodeFromKey(Key key)
        {
            // Direct cast is possible
            return (SDL_Scancode)key;
        }

        /// <summary>
        /// Gets the name of a key based on its physical location (scancode).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The name of the key, or an empty string if unknown.</returns>
        public static string GetKeyName(Key key)
        {
            return SDL_GetScancodeName(scancode: GetScancodeFromKey(key: key)) ?? string.Empty;
        }

        /// <summary>
        /// Gets the key corresponding to the given physical key name.
        /// </summary>
        /// <param name="name">The name of the key (e.g., "A", "Return", "Space").</param>
        /// <returns>The corresponding Key enum value.</returns>
        public static Key GetKeyFromName(string name)
        {
            return GetKeyFromScancode(scancode: SDL_GetScancodeFromName(name: name));
        }
    }

    #endregion

    #region Input Abstraction (Mouse)

    /// <summary>
    /// Represents specific mouse buttons, corresponding to SDL button indices.
    /// </summary>
    public enum MouseButton : byte // Using byte as the underlying type matches SDL_MouseButtonEvent.button
    {
        /// <summary>
        /// Represents an unknown or unhandled mouse button.
        /// </summary>
        Unknown = 0, // Good practice to have a default/unknown value

        /// <summary>
        /// The left mouse button (SDL_BUTTON_LEFT).
        /// </summary>
        Left = 1,

        /// <summary>
        /// The middle mouse button (SDL_BUTTON_MIDDLE).
        /// </summary>
        Middle = 2,

        /// <summary>
        /// The right mouse button (SDL_BUTTON_RIGHT).
        /// </summary>
        Right = 3,

        /// <summary>
        /// The first extra mouse button (typically "back") (SDL_BUTTON_X1).
        /// </summary>
        X1 = 4,

        /// <summary>
        /// The second extra mouse button (typically "forward") (SDL_BUTTON_X2).
        /// </summary>
        X2 = 5,

        // You could potentially add more if SDL supports them in the future,
        // but these are the standard ones.
    }

    /// <summary>
    /// Provides static methods for querying and controlling the mouse state.
    /// Requires the SDL Events subsystem to be initialized.
    /// </summary>
    public static class Mouse
    {
        /// <summary>
        /// Gets the current position of the mouse cursor relative to the focused window.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <param name="x">The x-coordinate of the mouse cursor.</param>
        /// <param name="y">The y-coordinate of the mouse cursor.</param>
        /// <returns>A bitmask of the current button state (SDL_BUTTON_LMASK, etc.). Use IsButtonDown for specific checks.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if there is an SDL error retrieving the state.</exception>
        public static SDL_MouseButtonFlags GetPosition(out float x, out float y)
        {
            EnsureEventsInitialized();

            // SDL_GetMouseState doesn't typically fail unless SDL isn't initialized properly,
            // which EnsureEventsInitialized should catch. We return the state directly.
            return SDL_GetMouseState(x: out x, y: out y);
        }

        /// <summary>
        /// Gets the current position of the mouse cursor relative to the focused window.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <returns>An FPoint representing the cursor position.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if there is an SDL error retrieving the state.</exception>
        public static FPoint GetPosition()
        {
            GetPosition(x: out float x, y: out float y);
            return new FPoint(x, y);
        }

        /// <summary>
        /// Gets the current position of the mouse cursor in global screen coordinates.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <param name="x">The global x-coordinate of the mouse cursor.</param>
        /// <param name="y">The global y-coordinate of the mouse cursor.</param>
        /// <returns>A bitmask of the current button state (SDL_BUTTON_LMASK, etc.).</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if there is an SDL error retrieving the state.</exception>
        public static SDL_MouseButtonFlags GetGlobalPosition(out float x, out float y)
        {
            EnsureEventsInitialized();

            // Similar to GetMouseState, errors are unlikely if initialized.
            return SDL_GetGlobalMouseState(x: out x, y: out y);
        }

        /// <summary>
        /// Gets the current position of the mouse cursor in global screen coordinates.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <returns>An FPoint representing the global cursor position.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if there is an SDL error retrieving the state.</exception>
        public static FPoint GetGlobalPosition()
        {
            GetGlobalPosition(x: out float x, y: out float y);
            return new FPoint(x, y);
        }

        /// <summary>
        /// Retrieves the relative motion of the mouse since the last call to PumpEvents or this function.
        /// </summary>
        /// <param name="xRel">The relative motion in the x-direction.</param>
        /// <param name="yRel">The relative motion in the y-direction.</param>
        /// <returns>A bitmask of the current button state.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        public static SDL_MouseButtonFlags GetRelativePosition(out float xRel, out float yRel)
        {
            EnsureEventsInitialized();
            // Note: SDL_GetRelativeMouseState resets the relative motion state internally.
            return SDL_GetRelativeMouseState(x: out xRel, y: out yRel);
        }

        /// <summary>
        /// Checks if a specific mouse button is currently held down using predefined masks.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <param name="button">The mouse button to check (e.g., MouseButton.Left).</param>
        /// <returns>True if the button is down, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an unhandled MouseButton value is provided.</exception>
        public static bool IsButtonDown(MouseButton button)
        {
            EnsureEventsInitialized();
            // Get the current state bitmask
            SDL_MouseButtonFlags state = SDL_GetMouseState(x: out _, y: out _);

            // Get the correct mask constant based on the button enum value
            var buttonMask = button switch
            {
                MouseButton.Left => SDL_MouseButtonFlags.SDL_BUTTON_LMASK, // Use the predefined mask
                MouseButton.Middle => SDL_MouseButtonFlags.SDL_BUTTON_MMASK, // Use the predefined mask
                MouseButton.Right => SDL_MouseButtonFlags.SDL_BUTTON_RMASK, // Use the predefined mask
                MouseButton.X1 => SDL_MouseButtonFlags.SDL_BUTTON_X1MASK, // Use the predefined mask
                MouseButton.X2 => SDL_MouseButtonFlags.SDL_BUTTON_X2MASK, // Use the predefined mask
                _ => throw new ArgumentOutOfRangeException(
                    nameof(button),
                    $"Unsupported mouse button: {button}"
                ), // Handle potential future buttons or invalid values if necessary
                // Option 1: Throw exception for unsupported buttons
            };

            // Check if the button's bit is set in the state mask
            return (state & buttonMask) != 0;
        }

        /// <summary>
        /// Checks if a specific mouse button is currently released.
        /// Requires SDL_PumpEvents() to have been called recently.
        /// </summary>
        /// <param name="button">The mouse button to check.</param>
        /// <returns>True if the button is up, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        public static bool IsButtonUp(MouseButton button)
        {
            return !IsButtonDown(button: button);
        }

        /// <summary>
        /// Sets whether relative mouse mode is enabled.
        /// When enabled, the cursor is hidden, and mouse motion provides relative changes (ideal for FPS controls).
        /// </summary>
        /// <param name="window">The window for which to set the relative mouse mode.</param>
        /// <param name="enabled">True to enable relative mode, false to disable.</param>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to set the mode.</exception>
        public static void SetRelativeMode(Window window, bool enabled)
        {
            EnsureEventsInitialized();
            SdlHost.ThrowOnFailure(
                result: SDL_SetWindowRelativeMouseMode(window: window.Handle, enabled: enabled),
                message: "Failed to set relative mouse mode"
            );
        }

        /// <summary>
        /// Gets whether relative mouse mode is currently enabled for the window.
        /// </summary>
        /// <returns>True if relative mode is enabled, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        public static bool GetRelativeModeEnabled(Window window)
        {
            EnsureEventsInitialized();
            return SDL_GetWindowRelativeMouseMode(window: window.Handle);
        }

        /// <summary>
        /// Shows the system cursor.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to show the cursor.</exception>
        public static void ShowCursor()
        {
            EnsureEventsInitialized();
            SdlHost.ThrowOnFailure(result: SDL_ShowCursor(), message: "Failed to show cursor");
        }

        /// <summary>
        /// Hides the system cursor.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to hide the cursor.</exception>
        public static void HideCursor()
        {
            EnsureEventsInitialized();
            SdlHost.ThrowOnFailure(result: SDL_HideCursor(), message: "Failed to hide cursor");
        }

        /// <summary>
        /// Gets whether the system cursor is currently shown.
        /// </summary>
        /// <returns>True if the cursor is shown, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        public static bool IsCursorShown()
        {
            EnsureEventsInitialized();
            return SDL_CursorVisible();
        }

        /// <summary>
        /// Moves the mouse cursor to the specified position within a window.
        /// </summary>
        /// <param name="window">The window to warp the cursor in.</param>
        /// <param name="x">The x-coordinate within the window.</param>
        /// <param name="y">The y-coordinate within the window.</param>
        /// <exception cref="ArgumentNullException">Thrown if window is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the window is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to warp the mouse.</exception>
        public static void WarpInWindow(Window window, float x, float y)
        {
            EnsureEventsInitialized();
            ArgumentNullException.ThrowIfNull(argument: window);
            ObjectDisposedException.ThrowIf(condition: window.IsDisposed, instance: window);

            SDL_WarpMouseInWindow(window: window.Handle, x: x, y: y);
        }

        /// <summary>
        /// Moves the mouse cursor to the specified global screen coordinates.
        /// </summary>
        /// <param name="x">The global x-coordinate.</param>
        /// <param name="y">The global y-coordinate.</param>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to warp the mouse.</exception>
        public static void WarpGlobal(float x, float y)
        {
            EnsureEventsInitialized();
            SdlHost.ThrowOnFailure(
                result: SDL_WarpMouseGlobal(x: x, y: y),
                message: "Failed to warp mouse globally"
            );
        }

        /// <summary>
        /// Enables mouse capture, restricting the cursor to the window boundaries.
        /// </summary>
        /// <param name="enabled">True to capture the mouse, false to release.</param>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        /// <exception cref="SDLException">Thrown if SDL fails to set the capture state.</exception>
        public static void CaptureMouse(bool enabled)
        {
            EnsureEventsInitialized();
            SdlHost.ThrowOnFailure(
                result: SDL_CaptureMouse(enabled: enabled),
                message: "Failed to set mouse capture state"
            );
        }

        /// <summary>
        /// Gets whether the window captures mouse input.
        /// </summary>
        /// <returns>The boolean if the window currently captures the mouse</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SDL Events subsystem is not initialized.</exception>
        public static bool GetMouseCaptureWindow(Window window)
        {
            EnsureEventsInitialized();
            return SDL_GetWindowMouseGrab(window: window.Handle);
        }

        /// <summary>
        /// Helper to ensure the Events subsystem is initialized.
        /// </summary>
        private static void EnsureEventsInitialized()
        {
            // Video subsystem often initializes Events, but check explicitly.
            if (
                !SdlHost.IsInitialized
                || (SdlHost.WasInit(flags: SdlSubSystem.Events) & SdlSubSystem.Events) == 0
            )
            {
                throw new InvalidOperationException(
                    "SDL Events subsystem not initialized. Call SdlHost.Init(SdlSubSystem.Events) or include it in your initial SdlHost.Init call."
                );
            }
        }
    }

    #endregion // Input Abstraction (Mouse)

    #region Event Handling

    /// <summary>
    /// Base class for SDL event arguments.
    /// </summary>
    public class SdlEventArgs(SDL_EventType type, ulong timestamp) : EventArgs
    {
        /// <summary>
        /// The timestamp of the event in milliseconds.
        /// </summary>
        public ulong Timestamp { get; } = timestamp;

        /// <summary>
        /// The type of the event.
        /// </summary>
        public SDL_EventType Type { get; } = type;
    }

    // --- Specific Event Argument Classes ---

    /// <summary>
    /// Event arguments for quit events.
    /// </summary>
    public class QuitEventArgs : SdlEventArgs
    {
        public QuitEventArgs(SDL_QuitEvent evt)
            : base(evt.type, evt.timestamp) { }
    }

    /// <summary>
    /// Event arguments for application lifecycle events.
    /// </summary>
    public class AppLifecycleEventArgs : SdlEventArgs
    {
        // No specific data beyond type and timestamp in SDL_CommonEvent
        public AppLifecycleEventArgs(SDL_CommonEvent common)
            : base((SDL_EventType)common.type, common.timestamp) { }
    }

    /// <summary>
    /// Event arguments for display events.
    /// </summary>
    public class DisplayEventArgs : SdlEventArgs
    {
        public uint DisplayId { get; }
        public int Data1 { get; } // Meaning depends on event type
        public int Data2 { get; } // Meaning depends on event type

        public DisplayEventArgs(SDL_DisplayEvent evt)
            : base(evt.type, evt.timestamp)
        {
            DisplayId = evt.displayID;
            Data1 = evt.data1;
            Data2 = evt.data2;
        }
    }

    /// <summary>
    /// Event arguments for window events. Includes abstracted event type.
    /// </summary>
    public class WindowEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public WindowEventType EventType { get; } // Wrapper enum for specific window event type
        public int Data1 { get; } // Meaning depends on event type (e.g., width for resize)
        public int Data2 { get; } // Meaning depends on event type (e.g., height for resize)

        // Internal constructor used by Events.MapEvent
        internal WindowEventArgs(SDL_WindowEvent evt, WindowEventType type)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            EventType = type; // Set the wrapper enum type
            Data1 = evt.data1;
            Data2 = evt.data2;
        }
    }

    /// <summary>
    /// Event arguments for keyboard device addition/removal.
    /// </summary>
    public class KeyboardDeviceEventArgs : SdlEventArgs
    {
        public uint Which { get; } // Instance ID

        public KeyboardDeviceEventArgs(SDL_KeyboardDeviceEvent evt)
            : base(evt.type, evt.timestamp)
        {
            Which = evt.which;
        }
    }

    /// <summary>
    /// Event arguments for keyboard key presses/releases.
    /// Uses the wrapper's Key and KeyModifier enums.
    /// </summary>
    public class KeyboardEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Keyboard instance ID
        public Key Key { get; } // Wrapper Key enum (physical location)
        public KeyModifier Modifiers { get; } // Wrapper KeyModifier enum
        public bool IsDown { get; }
        public bool IsRepeat { get; }

        // Removed: Scancode, Keycode, Raw (can be derived if needed)

        // FIX: Pass type and timestamp from the specific event struct, remove 'in'
        // Map SDL enums to wrapper enums here.
        public KeyboardEventArgs(SDL_KeyboardEvent evt)
            : base((SDL_EventType)evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            Key = Keyboard.GetKeyFromScancode(scancode: evt.scancode); // Use mapping function
            Modifiers = (KeyModifier)evt.mod; // Direct cast works for modifiers
            IsDown = evt.down;
            IsRepeat = evt.repeat;
        }
    }

    /// <summary>
    /// Event arguments for text input events.
    /// </summary>
    public class TextInputEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public string Text { get; }

        public unsafe TextInputEventArgs(SDL_TextInputEvent evt)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Text = Marshal.PtrToStringUTF8(ptr: (IntPtr)evt.text) ?? string.Empty;
        }
    }

    /// <summary>
    /// Event arguments for text editing events (IME composition).
    /// </summary>
    public class TextEditingEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public string Text { get; }
        public int Start { get; }
        public int Length { get; }

        public unsafe TextEditingEventArgs(SDL_TextEditingEvent evt)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Text = Marshal.PtrToStringUTF8(ptr: (IntPtr)evt.text) ?? string.Empty;
            Start = evt.start;
            Length = evt.length;
        }
    }

    /// <summary>
    /// Event arguments for mouse device addition/removal.
    /// </summary>
    public class MouseDeviceEventArgs : SdlEventArgs
    {
        public uint Which { get; } // Instance ID

        public MouseDeviceEventArgs(SDL_MouseDeviceEvent evt)
            : base(evt.type, evt.timestamp)
        {
            Which = evt.which;
        }
    }

    /// <summary>
    /// Event arguments for mouse motion events.
    /// </summary>
    public class MouseMotionEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public MouseButton State { get; }
        public float X { get; }
        public float Y { get; }
        public float XRel { get; }
        public float YRel { get; }

        public MouseMotionEventArgs(SDL_MouseMotionEvent evt)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            State = (MouseButton)evt.state;
            X = evt.x;
            Y = evt.y;
            XRel = evt.xrel;
            YRel = evt.yrel;
        }
    }

    /// <summary>
    /// Event arguments for mouse button presses/releases.
    /// </summary>
    public class MouseButtonEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public MouseButton Button { get; } // Button index (1=left, 2=middle, 3=right, etc.)
        public bool IsDown { get; }
        public byte Clicks { get; } // 1 for single-click, 2 for double-click, etc.
        public float X { get; }
        public float Y { get; }

        public MouseButtonEventArgs(SDL_MouseButtonEvent evt)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            Button = (MouseButton)evt.button;
            IsDown = evt.down;
            Clicks = evt.clicks;
            X = evt.x;
            Y = evt.y;
        }
    }

    /// <summary>
    /// Event arguments for mouse wheel events.
    /// </summary>
    public class MouseWheelEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public float ScrollX { get; }
        public float ScrollY { get; }
        public SDL_MouseWheelDirection Direction { get; }
        public float MouseX { get; } // Precise mouse coordinates at time of scroll
        public float MouseY { get; } // Precise mouse coordinates at time of scroll

        public MouseWheelEventArgs(SDL_MouseWheelEvent evt)
            : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            ScrollX = evt.x;
            ScrollY = evt.y;
            Direction = evt.direction;
            MouseX = evt.mouse_x;
            MouseY = evt.mouse_y;
        }
    }

    // TODO: Add event args classes for Joystick, Gamepad, Touch, Sensor, Drop, Clipboard, User, etc.

    /// <summary>
    /// Provides static methods for handling SDL events.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Pumps the event loop, gathering events from the input devices.
        /// </summary>
        public static void PumpEvents()
        {
            SDL_PumpEvents();
        }

        /// <summary>
        /// Polls for currently pending events.
        /// </summary>
        /// <param name="eventArgs">The event arguments if an event was pending, otherwise null.</param>
        /// <returns>True if an event was polled, false otherwise.</returns>
        public static bool PollEvent(out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_PollEvent(out SDL_Event sdlEvent))
            {
                eventArgs = MapEvent(sdlEvent: sdlEvent);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Waits indefinitely for the next available event.
        /// </summary>
        /// <param name="eventArgs">The event arguments for the event that occurred.</param>
        /// <returns>True if an event was received, false on error.</returns>
        public static bool WaitEvent(out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_WaitEvent(out SDL_Event sdlEvent))
            {
                eventArgs = MapEvent(sdlEvent: sdlEvent);
                return true;
            }
            // SDL_WaitEvent returns false on error
            SdlHost.ClearError(); // Clear error potentially set by WaitEvent failure
            return false;
        }

        /// <summary>
        /// Waits until the specified timeout for the next available event.
        /// </summary>
        /// <param name="timeoutMs">The maximum number of milliseconds to wait.</param>
        /// <param name="eventArgs">The event arguments if an event was available, otherwise null.</param>
        /// <returns>True if an event was received, false if the timeout elapsed or an error occurred.</returns>
        public static bool WaitEventTimeout(int timeoutMs, out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_WaitEventTimeout(out SDL_Event sdlEvent, timeoutMS: timeoutMs))
            {
                eventArgs = MapEvent(sdlEvent: sdlEvent);
                return true;
            }
            // SDL_WaitEventTimeout returns false on timeout or error
            SdlHost.ClearError(); // Clear error potentially set by WaitEventTimeout failure
            return false;
        }

        /// <summary>
        /// Maps the raw SDL_Event structure to a managed SdlEventArgs object.
        /// </summary>
        /// <param name="sdlEvent">The raw SDL event.</param>
        /// <returns>A corresponding SdlEventArgs object, or null if the event type is unhandled.</returns>
        public static SdlEventArgs? MapEvent(SDL_Event sdlEvent)
        {
            SDL_EventType type = (SDL_EventType)sdlEvent.type;
            switch (type)
            {
                // Application Events
                case SDL_EventType.SDL_EVENT_QUIT:
                    return new QuitEventArgs(sdlEvent.quit);
                case SDL_EventType.SDL_EVENT_TERMINATING:
                case SDL_EventType.SDL_EVENT_LOW_MEMORY:
                case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
                case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                case SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND:
                case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
                case SDL_EventType.SDL_EVENT_LOCALE_CHANGED:
                case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED:
                    return new AppLifecycleEventArgs(sdlEvent.common);

                // Display Events
                case SDL_EventType.SDL_EVENT_DISPLAY_ORIENTATION:
                case SDL_EventType.SDL_EVENT_DISPLAY_ADDED:
                case SDL_EventType.SDL_EVENT_DISPLAY_REMOVED:
                case SDL_EventType.SDL_EVENT_DISPLAY_MOVED:
                case SDL_EventType.SDL_EVENT_DISPLAY_DESKTOP_MODE_CHANGED:
                case SDL_EventType.SDL_EVENT_DISPLAY_CURRENT_MODE_CHANGED:
                case SDL_EventType.SDL_EVENT_DISPLAY_CONTENT_SCALE_CHANGED:
                    return new DisplayEventArgs(sdlEvent.display);

                // Window Events
                // Window Events - Map to specific WindowEventType
                case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Shown);
                case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Hidden);
                case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Exposed);
                case SDL_EventType.SDL_EVENT_WINDOW_MOVED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Moved);
                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Resized);
                case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.PixelSizeChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Minimized);
                case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Maximized);
                case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Restored);
                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.MouseEnter);
                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.MouseLeave);
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.FocusGained);
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.FocusLost);
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.CloseRequested);
                case SDL_EventType.SDL_EVENT_WINDOW_HIT_TEST:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.HitTest);
                case SDL_EventType.SDL_EVENT_WINDOW_ICCPROF_CHANGED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.IccProfileChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.DisplayChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                    return new WindowEventArgs(
                        sdlEvent.window,
                        WindowEventType.DisplayScaleChanged
                    );
                case SDL_EventType.SDL_EVENT_WINDOW_OCCLUDED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Occluded);
                case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.EnterFullscreen);
                case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.LeaveFullscreen);
                case SDL_EventType.SDL_EVENT_WINDOW_DESTROYED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.Destroyed);
                case SDL_EventType.SDL_EVENT_WINDOW_SAFE_AREA_CHANGED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.SafeAreaChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_HDR_STATE_CHANGED:
                    return new WindowEventArgs(sdlEvent.window, WindowEventType.HdrStateChanged);

                // Keyboard Events
                case SDL_EventType.SDL_EVENT_KEYBOARD_ADDED:
                case SDL_EventType.SDL_EVENT_KEYBOARD_REMOVED:
                    return new KeyboardDeviceEventArgs(sdlEvent.kdevice);
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    return new KeyboardEventArgs(sdlEvent.key);
                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                    return new TextInputEventArgs(sdlEvent.text);
                case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                    return new TextEditingEventArgs(sdlEvent.edit);
                case SDL_EventType.SDL_EVENT_KEYMAP_CHANGED:
                    // Maybe a specific event args or just use CommonEvent?
                    return new SdlEventArgs(type, sdlEvent.common.timestamp) { };

                // Mouse Events
                case SDL_EventType.SDL_EVENT_MOUSE_ADDED:
                case SDL_EventType.SDL_EVENT_MOUSE_REMOVED:
                    return new MouseDeviceEventArgs(sdlEvent.mdevice);
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    return new MouseMotionEventArgs(sdlEvent.motion);
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    return new MouseButtonEventArgs(sdlEvent.button);
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    return new MouseWheelEventArgs(sdlEvent.wheel);

                // TODO: Add mappings for Joystick, Gamepad, Touch, Sensor, Drop, Clipboard, User events etc.

                default:
                    // Log unhandled event?
                    Console.WriteLine(
                        value: $"Warning: Unhandled SDL Event Type: {type} ({(uint)type})"
                    );
                    return null; // Or return a generic SdlEventArgs(sdlEvent.common)?
            }
        }

        /// <summary>
        /// Sets the state of processing events for a specific type.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <param name="enabled">True to process events, false to ignore them.</param>
        public static void SetEventEnabled(SDL_EventType type, bool enabled)
        {
            SDL_SetEventEnabled(type: (uint)type, enabled: enabled);
        }

        /// <summary>
        /// Checks if an event type is enabled for processing.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <returns>True if the event type is enabled, false otherwise.</returns>
        public static bool IsEventEnabled(SDL_EventType type)
        {
            return SDL_EventEnabled(type: (uint)type);
        }

        /// <summary>
        /// Clears events of a specific type from the event queue.
        /// </summary>
        /// <param name="type">The type of event to clear.</param>
        public static void FlushEvent(SDL_EventType type)
        {
            SDL_FlushEvent(type: (uint)type);
        }

        /// <summary>
        /// Clears events within a range of types from the event queue.
        /// </summary>
        /// <param name="minType">The minimum event type to clear.</param>
        /// <param name="maxType">The maximum event type to clear.</param>
        public static void FlushEvents(SDL_EventType minType, SDL_EventType maxType)
        {
            SDL_FlushEvents(minType: (uint)minType, maxType: (uint)maxType);
        }

        // Internal cleanup if needed (e.g., unregistering event watches)
        internal static void Quit()
        {
            // Add cleanup here if necessary
        }
    }

    #endregion
} // namespace SDL3.Wrapper
