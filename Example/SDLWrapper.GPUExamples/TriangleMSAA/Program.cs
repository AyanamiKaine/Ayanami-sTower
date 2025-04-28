using AyanamisTower.NihilEx.SDLWrapper;
using static SDL3.SDL;

namespace TriangleMSAA;

/// <summary>
/// This is a port of https://github.com/TheSpydog/SDL_gpu_examples/blob/main/Examples/TriangleMSAA.c
/// using my SDL3 wrapper around the native SDL3 bindings, now utilizing the GraphicsPipelineBuilder
/// and demonstrating MSAA (Multisample Anti-Aliasing).
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
    private static GpuCommandBuffer? commadBuffer = null;
    private static bool _shouldQuit = false;

    // MSAA related resources
    private static readonly Dictionary<SDL_GPUSampleCount, GpuGraphicsPipeline> msaaPipelines =
        new();
    private static readonly Dictionary<SDL_GPUSampleCount, GpuTexture> msaaRenderTargets = new();
    private static GpuTexture? resolveTexture = null; // Single-sampled texture for resolving MSAA targets
    private static readonly List<SDL_GPUSampleCount> supportedSampleCounts = new();
    private static int currentSampleCountIndex = 0;
    private static SDL_GPUTextureFormat swapchainFormat =
        SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;

    // Define fixed internal resolution for MSAA targets
    private const uint InternalWidth = 640;
    private const uint InternalHeight = 480;

    private static unsafe void Main()
    {
        try // Add basic error handling for initialization
        {
            Init();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Initialization failed: {ex}");
            Cleanup(); // Attempt cleanup even if init fails
            return;
        }

        // Print instructions after successful init
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("Press LEFT/RIGHT arrow keys to cycle MSAA sample counts.");
        LogCurrentSampleCount();
        Console.WriteLine("-----------------------------------------");

        while (!_shouldQuit)
        {
            // --- Event Handling ---
            while (Events.PollEvent(out SdlEventArgs? evt))
            {
                // Handle global quit event first
                if (evt is QuitEventArgs)
                {
                    Console.WriteLine(value: "Quit event received.");
                    _shouldQuit = true;
                    break; // Exit event loop if quitting
                }
                else if (evt is KeyboardEventArgs keyEvt && keyEvt.IsDown) // Handle key presses
                {
                    HandleKeyPress(keyEvt.Key);
                }
                // Add other event handling if needed (e.g., window resize - might need recreating MSAA/resolve textures)
            }
            if (_shouldQuit)
                break; // Exit main loop if quit requested

            // Check if window or device became invalid
            if (window == null || window.IsDisposed || gpuDevice == null || gpuDevice.IsDisposed)
            {
                Console.Error.WriteLine("Window or GPU device became invalid.");
                _shouldQuit = true;
                continue;
            }

            // --- Drawing ---
            try
            {
                Draw();
            }
            catch (ObjectDisposedException ode)
            {
                Console.Error.WriteLine(
                    $"Draw failed because an object was disposed: {ode.ObjectName}"
                );
                _shouldQuit = true; // Likely need to exit if resources are gone
            }
            catch (SDLException sdlEx)
            {
                Console.Error.WriteLine($"SDL Error during Draw: {sdlEx.Message}");
                // Potentially try to recover or just quit
                _shouldQuit = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Draw failed: {ex}");
                _shouldQuit = true; // Exit on other draw errors
            }
        }

        Cleanup();
    }

    private static void HandleKeyPress(Key key)
    {
        bool changed = false;
        if (key == Key.Left)
        {
            currentSampleCountIndex--;
            if (currentSampleCountIndex < 0)
            {
                currentSampleCountIndex = supportedSampleCounts.Count - 1;
            }
            changed = true;
        }
        else if (key == Key.Right)
        {
            currentSampleCountIndex = (currentSampleCountIndex + 1) % supportedSampleCounts.Count;
            changed = true;
        }

        if (changed)
        {
            LogCurrentSampleCount();
        }
    }

    private static void LogCurrentSampleCount()
    {
        if (
            supportedSampleCounts.Count > 0
            && currentSampleCountIndex >= 0
            && currentSampleCountIndex < supportedSampleCounts.Count
        )
        {
            SDL_GPUSampleCount currentCountEnum = supportedSampleCounts[currentSampleCountIndex];
            int currentCountValue = 1 << (int)currentCountEnum; // Calculate 2^enum_value
            Console.WriteLine(
                $"Current sample count: {currentCountValue}x (Enum: {currentCountEnum})"
            );
        }
    }

    private static unsafe void Init()
    {
        SdlHost.Init(SdlSubSystem.Video | SdlSubSystem.Events); // Ensure Events are initialized
        window = new Window("MSAA Triangle Example (Builder)", 800, 600, WindowFlags.Resizable); // Slightly larger window
        gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true // Enable debug layers if possible
        );
        gpuDevice.ClaimWindow(window);

        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");

        // --- Load Shaders ---
        GpuShader vertexShader = GpuShader.LoadShader(gpuDevice, "Shader/RawTriangle.vert");
        GpuShader fragmentShader = GpuShader.LoadShader(gpuDevice, "Shader/SolidColor.frag");

        // --- Determine Swapchain Format and Supported Sample Counts ---
        swapchainFormat = gpuDevice.GetSwapchainTextureFormat(window);
        if (swapchainFormat == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID)
        {
            throw new SDLException("Could not get swapchain texture format.");
        }
        Console.WriteLine($"Swapchain format: {swapchainFormat}");

        // Check support for 1x, 2x, 4x, 8x MSAA
        SDL_GPUSampleCount[] possibleSampleCounts =
        [
            SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
            SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_2,
            SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_4,
            SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_8,
        ];

        Console.WriteLine("Checking supported sample counts...");
        foreach (var sampleCount in possibleSampleCounts)
        {
            if (gpuDevice.SupportsSampleCount(swapchainFormat, sampleCount))
            {
                supportedSampleCounts.Add(sampleCount);
                Console.WriteLine($"  - {(1 << (int)sampleCount)}x supported.");
            }
            else
            {
                Console.WriteLine($"  - {(1 << (int)sampleCount)}x NOT supported.");
            }
        }

        if (supportedSampleCounts.Count == 0)
        {
            // Should always support at least 1x
            throw new NotSupportedException("No supported sample counts found (not even 1x?).");
        }
        // Ensure 1x is first if present (it should be)
        supportedSampleCounts.Sort();
        currentSampleCountIndex = supportedSampleCounts.IndexOf(
            SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1
        );
        if (currentSampleCountIndex < 0)
            currentSampleCountIndex = 0; // Fallback if 1x wasn't found somehow

        // --- Create Pipelines and MSAA Render Targets ---
        Console.WriteLine("Creating pipelines and MSAA render targets...");
        try
        {
            foreach (var sampleCount in supportedSampleCounts)
            {
                Console.WriteLine($"  Creating resources for {(1 << (int)sampleCount)}x...");
                // Create Pipeline
                var pipelineBuilder = new GraphicsPipelineBuilder(gpuDevice)
                    .SetVertexShader(vertexShader)
                    .SetFragmentShader(fragmentShader)
                    .SetPrimitiveType(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST)
                    .AddColorTarget(swapchainFormat) // Target format is the same
                    .SetFillMode(SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL)
                    .SetCullMode(SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE)
                    .SetFrontFace(SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE)
                    .EnableDepthTest(false)
                    .EnableStencilTest(false)
                    .SetSampleCount(sampleCount) // Set the specific sample count for this pipeline
                    .SetName($"TrianglePipeline_{(1 << (int)sampleCount)}x");

                msaaPipelines[sampleCount] = pipelineBuilder.Build();

                // Create MSAA Render Target
                SDL_GPUTextureUsageFlags usage =
                    SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
                // Only add SAMPLER usage if it's the 1x target, as it might be blitted directly
                if (sampleCount == SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1)
                {
                    usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;
                }

                var msaaTexture = gpuDevice.CreateTexture(
                    new SDL_GPUTextureCreateInfo
                    {
                        type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
                        format = swapchainFormat,
                        usage = usage,
                        width = InternalWidth,
                        height = InternalHeight,
                        layer_count_or_depth = 1,
                        num_levels = 1,
                        sample_count = sampleCount, // Use the specific sample count
                        props =
                            0 // No extra properties needed here
                        ,
                    }
                );
                msaaTexture.SetName($"MSAARenderTarget_{(1 << (int)sampleCount)}x");
                msaaRenderTargets[sampleCount] = msaaTexture;

                Console.WriteLine(
                    $"    Pipeline and Render Target created for {(1 << (int)sampleCount)}x."
                );
            }

            // --- Create Resolve Texture ---
            Console.WriteLine("Creating resolve texture...");
            resolveTexture = gpuDevice.CreateTexture(
                new SDL_GPUTextureCreateInfo
                {
                    type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
                    format = swapchainFormat,
                    // Must be usable as a resolve target (implicitly, by being COLOR_TARGET)
                    // AND as a sampler source for the final blit.
                    usage =
                        SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET
                        | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
                    width = InternalWidth,
                    height = InternalHeight,
                    layer_count_or_depth = 1,
                    num_levels = 1,
                    sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1, // Resolve target is always 1x
                    props = 0,
                }
            );
            resolveTexture.SetName("ResolveTexture");
            Console.WriteLine("Resolve texture created.");
        }
        catch
        {
            // Clean up partially created resources on failure
            Cleanup(); // Use the main cleanup function
            throw; // Re-throw the exception
        }
        finally // Ensure shaders are disposed even if pipeline/texture creation fails later
        {
            vertexShader?.Dispose();
            fragmentShader?.Dispose();
        }

        Console.WriteLine("Initialization complete.");
    }

    private static void Draw()
    {
        // Acquire command buffer
        if (gpuDevice == null || gpuDevice.IsDisposed)
            return;
        commadBuffer = gpuDevice.AcquireCommandBuffer();

        IntPtr swapchainTextureHandle = IntPtr.Zero;
        uint swapchainWidth = 0,
            swapchainHeight = 0;

        // Acquire swapchain texture
        if (
            !commadBuffer.WaitAndAcquireSwapchainTexture(
                window!,
                out swapchainTextureHandle,
                out swapchainWidth,
                out swapchainHeight
            )
        )
        {
            Console.WriteLine("Failed to acquire swapchain texture.");
            commadBuffer.Cancel();
            return;
        }

        // Get current MSAA settings
        SDL_GPUSampleCount currentSampleCount = supportedSampleCounts[currentSampleCountIndex];
        GpuGraphicsPipeline currentPipeline = msaaPipelines[currentSampleCount];
        GpuTexture currentRenderTarget = msaaRenderTargets[currentSampleCount];
        bool useResolve = currentSampleCount != SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;

        // --- Step 1: Render Pass to MSAA Target (or 1x Target) ---
        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = currentRenderTarget.Handle,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            clear_color = new FColor(0.1f, 0.2f, 0.3f, 1.0f), // Background color
            cycle =
                false // Not cycling offscreen targets
            ,
        };

        if (useResolve)
        {
            // If MSAA > 1x, resolve to the dedicated resolve texture
            colorTargetInfo.store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_RESOLVE;
            colorTargetInfo.resolve_texture = resolveTexture!.Handle; // Should not be null if init succeeded
            colorTargetInfo.cycle_resolve_texture = false; // Not cycling resolve target
        }
        else
        {
            // If 1x sampling, just store the result directly in the 1x target
            colorTargetInfo.store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE;
            colorTargetInfo.resolve_texture = IntPtr.Zero; // No resolve needed
        }

        Span<SDL_GPUColorTargetInfo> colorTargets = [colorTargetInfo];

        using (var renderPass = commadBuffer.BeginRenderPass(colorTargets)) // No depth/stencil
        {
            renderPass.BindGraphicsPipeline(currentPipeline);

            // Set viewport/scissor for the *internal* render target size
            renderPass.SetViewport(
                new SDL_GPUViewport
                {
                    x = 0,
                    y = 0,
                    w = InternalWidth,
                    h = InternalHeight,
                    min_depth = 0.0f,
                    max_depth = 1.0f,
                }
            );
            renderPass.SetScissor(new Rect(0, 0, (int)InternalWidth, (int)InternalHeight));

            // Draw the triangle
            renderPass.DrawPrimitives(3, 1, 0, 0);
        } // Render pass ends

        // --- Step 2: Blit the result to the Swapchain ---
        // Determine which texture holds the final image for this frame
        GpuTexture sourceTextureForBlit = useResolve ? resolveTexture! : currentRenderTarget; // Use resolveTexture if MSAA was active, else the 1x RT

        // Create the BlitInfo struct explicitly
        SDL_GPUBlitInfo blitInfo = new SDL_GPUBlitInfo
        {
            source = new SDL_GPUBlitRegion
            {
                texture = sourceTextureForBlit.Handle,
                x = 0,
                y = 0,
                w = InternalWidth,
                h = InternalHeight, // Source is the full internal texture
                mip_level = 0,
                layer_or_depth_plane = 0,
            },
            destination = new SDL_GPUBlitRegion
            {
                texture = swapchainTextureHandle,
                x = 0,
                y = 0,
                w = swapchainWidth,
                h = swapchainHeight, // Destination is the full swapchain texture
                mip_level = 0,
                layer_or_depth_plane = 0,
            },
            filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR, // Use linear for scaling if window size != internal size
            flip_mode = SDL_FlipMode.SDL_FLIP_NONE,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE, // Don't need to clear swapchain before blit
            cycle =
                false // Keep cycle false for Blit destination
            ,
        };

        // Perform the blit directly on the command buffer *after* the render pass
        // Assumes you added BlitTexture(in SDL_GPUBlitInfo info) to your GpuCommandBuffer class
        commadBuffer.BlitTexture(blitInfo);

        // Submit the command buffer
        commadBuffer.Submit();
    }

    private static void Cleanup()
    {
        Console.WriteLine("Cleaning up resources...");

        // Dispose MSAA resources
        foreach (var pipeline in msaaPipelines.Values)
        {
            pipeline?.Dispose();
        }
        msaaPipelines.Clear();

        foreach (var target in msaaRenderTargets.Values)
        {
            target?.Dispose();
        }
        msaaRenderTargets.Clear();

        resolveTexture?.Dispose();
        resolveTexture = null;

        supportedSampleCounts.Clear();

        // Release window claim before destroying device
        if (gpuDevice != null && !gpuDevice.IsDisposed && window != null && !window.IsDisposed)
        {
            try
            {
                gpuDevice.ReleaseWindow(window);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error releasing window: {ex.Message}");
            }
        }

        gpuDevice?.Dispose(); // Dispose GPU device
        gpuDevice = null;

        window?.Dispose(); // Dispose window
        window = null;

        SdlHost.Quit(); // Quit SDL subsystems
        Console.WriteLine("Cleanup complete.");
    }
}
