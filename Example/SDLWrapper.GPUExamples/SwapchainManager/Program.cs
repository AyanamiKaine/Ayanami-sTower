using System;
using System.Collections.Generic; // Needed for List/Dictionary
using System.Linq; // Needed for LINQ methods like Max
using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;

namespace SwapchainManagerExample;

/// <summary>
/// Demonstrates MSAA using the GpuSwapchainManager abstraction.
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
    private static GpuSwapchainManager? swapchainManager = null; // Use the manager
    private static GpuGraphicsPipeline? fillPipeline = null; // Only one pipeline needed now
    private static bool _shouldQuit = false;

    // Track desired MSAA level
    private static readonly SDL_GPUSampleCount[] availableMsaaLevels =
    [
        SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_2,
        SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_4,
        SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_8,
    ];
    private static int currentMsaaLevelIndex = 0; // Index into availableMsaaLevels

    // Define fixed internal resolution for MSAA targets - CAN BE DIFFERENT FROM WINDOW
    private const uint InternalWidth = 1280;
    private const uint InternalHeight = 720;

    private static void Main()
    {
        try
        {
            Init();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Initialization failed: {ex}");
            Cleanup();
            return;
        }

        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("Press LEFT/RIGHT arrow keys to cycle MSAA sample counts.");
        LogCurrentSampleCount();
        Console.WriteLine("-----------------------------------------");

        while (!_shouldQuit)
        {
            while (Events.PollEvent(out SdlEventArgs? evt))
            {
                if (evt is QuitEventArgs)
                {
                    Console.WriteLine("Quit event received.");
                    _shouldQuit = true;
                    break;
                }
                else if (evt is KeyboardEventArgs keyEvt && keyEvt.IsDown)
                {
                    HandleKeyPress(keyEvt.Key);
                }
                // TODO: Handle window resize - would likely require calling swapchainManager.SetMSAA again
                // to recreate textures with the new size if using window size for internal resolution.
            }
            if (_shouldQuit)
                break;

            if (
                window == null
                || window.IsDisposed
                || gpuDevice == null
                || gpuDevice.IsDisposed
                || swapchainManager == null
            )
            {
                Console.Error.WriteLine("Window, GPU device, or SwapchainManager became invalid.");
                _shouldQuit = true;
                continue;
            }

            try
            {
                Draw();
            }
            catch (ObjectDisposedException ode)
            {
                Console.Error.WriteLine(
                    $"Draw failed because an object was disposed: {ode.ObjectName}"
                );
                _shouldQuit = true;
            }
            catch (SDLException sdlEx)
            {
                Console.Error.WriteLine($"SDL Error during Draw: {sdlEx.Message}");
                _shouldQuit = true; // Often fatal
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Draw failed: {ex}");
                _shouldQuit = true;
            }
        }

        Cleanup();
    }

    private static void HandleKeyPress(Key key)
    {
        bool changed = false;
        SDL_GPUSampleCount desiredLevel;

        if (key == Key.Left)
        {
            currentMsaaLevelIndex--;
            if (currentMsaaLevelIndex < 0)
            {
                currentMsaaLevelIndex = availableMsaaLevels.Length - 1;
            }
            changed = true;
        }
        else if (key == Key.Right)
        {
            currentMsaaLevelIndex = (currentMsaaLevelIndex + 1) % availableMsaaLevels.Length;
            changed = true;
        }

        if (changed)
        {
            desiredLevel = availableMsaaLevels[currentMsaaLevelIndex];
            try
            {
                // Tell the manager to update its targets for the new level
                // This might recreate textures if the actual supported level changes
                swapchainManager?.SetMSAA(desiredLevel, InternalWidth, InternalHeight);

                // Recreate the pipeline to match the manager's *actual* sample count
                RecreatePipeline(); // Need a helper for this
                LogCurrentSampleCount();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to set MSAA level or recreate pipeline: {ex}");
                // Optionally revert index or quit
                _shouldQuit = true;
            }
        }
    }

    private static void LogCurrentSampleCount()
    {
        if (swapchainManager != null)
        {
            int currentCountValue = 1 << (int)swapchainManager.ActualSampleCount;
            Console.WriteLine(
                $"Current sample count: {currentCountValue}x (Enum: {swapchainManager.ActualSampleCount})"
            );
        }
    }

    private static void Init()
    {
        SdlHost.Init(SdlSubSystem.Video | SdlSubSystem.Events);
        window = new Window("MSAA Triangle (SwapchainManager)", 800, 600, WindowFlags.Resizable);
        gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true
        );
        gpuDevice.ClaimWindow(window);
        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");

        // Create the swapchain manager
        swapchainManager = new GpuSwapchainManager(gpuDevice, window);

        // Set initial desired MSAA level (e.g., try for 8x)
        // This also creates the initial internal textures
        currentMsaaLevelIndex = availableMsaaLevels.Length - 1; // Start at highest desired
        swapchainManager.SetMSAA(
            availableMsaaLevels[currentMsaaLevelIndex],
            InternalWidth,
            InternalHeight
        );

        // Create the initial pipeline matching the *actual* supported sample count
        RecreatePipeline();

        Console.WriteLine("Initialization complete.");
    }

    // Helper to create/recreate the pipeline based on the swapchain manager's state
    private static void RecreatePipeline()
    {
        fillPipeline?.Dispose(); // Dispose old one if it exists
        fillPipeline = null;

        if (gpuDevice == null || swapchainManager == null)
            return; // Should not happen if called after Init

        Console.WriteLine($"Recreating pipeline for {swapchainManager.ActualSampleCount}...");

        // --- Load Shaders (Need them again here) ---
        // Consider caching these if loading is slow, but for simplicity, load again.
        GpuShader? vertexShader = null;
        GpuShader? fragmentShader = null;
        try
        {
            vertexShader = GpuShader.LoadShader(gpuDevice, "Shader/RawTriangle.vert");
            fragmentShader = GpuShader.LoadShader(gpuDevice, "Shader/SolidColor.frag");

            var pipelineBuilder = new GraphicsPipelineBuilder(gpuDevice)
                .SetVertexShader(vertexShader)
                .SetFragmentShader(fragmentShader)
                .SetPrimitiveType(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST)
                // Use format and sample count provided by the manager
                .AddColorTarget(swapchainManager.RenderTargetFormat)
                .SetSampleCount(swapchainManager.ActualSampleCount) // CRITICAL: Match the manager's actual count
                .SetFillMode(SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL)
                .SetCullMode(SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE)
                .SetFrontFace(SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE)
                .EnableDepthTest(false) // No depth needed for this example
                .EnableStencilTest(false)
                .SetName($"TrianglePipeline_{swapchainManager.ActualSampleCount}");

            fillPipeline = pipelineBuilder.Build();
            Console.WriteLine("Pipeline recreated successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed during pipeline recreation: {ex}");
            throw; // Propagate error
        }
        finally
        {
            // Dispose shaders loaded for pipeline creation
            vertexShader?.Dispose();
            fragmentShader?.Dispose();
        }
    }

    private static void Draw()
    {
        // --- Acquire Frame ---
        FrameInfo? frameInfo = swapchainManager?.AcquireNextFrame();
        if (frameInfo == null)
        {
            // Acquisition failed (e.g., window closed)
            return;
        }

        // --- Begin Render Pass ---
        // The manager provides the correct target (MSAA or single-sampled)
        // and handles resolve setup internally when BeginRenderPass is called.
        using (
            var renderPass = swapchainManager!.BeginRenderPass(
                frameInfo,
                SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                new FColor(0.1f, 0.2f, 0.3f, 1.0f) // Background color
            /* No depth/stencil info needed */)
        )
        {
            renderPass.BindGraphicsPipeline(fillPipeline!); // Bind the pipeline matching the sample count

            // Set viewport/scissor for the *internal* render target size
            renderPass.SetViewport(
                new SDL_GPUViewport
                {
                    x = 0,
                    y = 0,
                    w = swapchainManager.InternalWidth,
                    h = swapchainManager.InternalHeight,
                    min_depth = 0.0f,
                    max_depth = 1.0f,
                }
            );
            renderPass.SetScissor(
                new Rect(
                    0,
                    0,
                    (int)swapchainManager.InternalWidth,
                    (int)swapchainManager.InternalHeight
                )
            );

            // Draw the triangle
            renderPass.DrawPrimitives(3, 1, 0, 0);
        } // Render pass ends

        // --- Present Frame ---
        // The manager handles the blit from the internal target (or resolved target)
        // to the swapchain and submits the command buffer.
        swapchainManager.PresentFrame(frameInfo);
    }

    private static void Cleanup()
    {
        Console.WriteLine("Cleaning up resources...");

        fillPipeline?.Dispose(); // Dispose pipeline
        fillPipeline = null;

        swapchainManager?.Dispose(); // Dispose manager (which disposes its textures)
        swapchainManager = null;

        // Device and Window are disposed AFTER manager
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

        gpuDevice?.Dispose();
        gpuDevice = null;

        window?.Dispose();
        window = null;

        SdlHost.Quit();
        Console.WriteLine("Cleanup complete.");
    }
}
