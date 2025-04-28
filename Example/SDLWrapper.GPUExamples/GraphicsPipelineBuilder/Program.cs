using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;

namespace GraphicsPipelineBuilderExample;

/// <summary>
/// This is a an example of using my pipelinebuilder abstraction.
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
    private static GpuGraphicsPipeline? fillPipeline = null;
    private static GpuCommandBuffer? commadBuffer = null;
    private static bool _shouldQuit = false;

    private static void Main()
    {
        Init();
        while (!_shouldQuit)
        {
            while (Events.PollEvent(out SdlEventArgs? evt))
            {
                // Handle global quit event first
                if (evt is QuitEventArgs)
                {
                    Console.WriteLine(value: "Quit event received.");
                    _shouldQuit = true;
                }
            }
            Draw();
        }
    }

    private static void Init()
    {
        SdlHost.Init(SdlSubSystem.Everything);
        window = new Window("GraphicsPipelineBuilder Example", 400, 400, WindowFlags.Resizable);
        gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true
        );
        gpuDevice.ClaimWindow(window);

        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");

        GpuShader vertexShader = GpuShader.LoadShader(
            gpuDevice,
            "Shader/RawTriangle.vert",
            0,
            0,
            0,
            0
        );

        GpuShader fragmentShader = GpuShader.LoadShader(
            gpuDevice,
            "Shader/SolidColor.frag",
            0,
            0,
            0,
            0
        );
        SDL_GPUTextureFormat swapchainFormat = gpuDevice.GetSwapchainTextureFormat(window);
        if (swapchainFormat == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID)
        {
            throw new SDLException("Could not get swapchain texture format.");
        }
        Console.WriteLine($"Using swapchain format: {swapchainFormat}");

        var pipelineBuilder = new GraphicsPipelineBuilder(gpuDevice)
            .SetVertexShader(vertexShader)
            .SetFragmentShader(fragmentShader)
            .SetPrimitiveType(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST)
            // Add the color target using the swapchain format
            .AddColorTarget(swapchainFormat) // Uses default blend state (no blend)
            // Set rasterizer state (optional, defaults are often okay)
            .SetFillMode(SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL) // Or SDL_GPU_FILLMODE_LINE for wireframe
            .SetCullMode(SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE) // No culling for a simple triangle
            .SetFrontFace(SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE)
            // Depth/Stencil state (optional, defaults to disabled)
            .EnableDepthTest(false)
            .EnableStencilTest(false)
            // Multisample state (optional, defaults to 1 sample)
            // We are directly targeting the SwapChainTexture from the window, its always
            // created with the samplecount of 1, if we set the sample count in our render
            // pipeline to anything else it wont work. See the TriangleMSAA example for more.
            .SetSampleCount(SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1)
            // Vertex Input State (optional for this shader as it uses built-ins)
            // If your shader used vertex attributes, you'd add them here:
            // .AddVertexBuffer(...)
            // .AddVertexAttribute(...)
            .SetName("BasicTrianglePipeline"); // Optional debug name

        fillPipeline = pipelineBuilder.Build();
    }

    private static void Draw()
    {
        commadBuffer = gpuDevice?.AcquireCommandBuffer();
        IntPtr swapchainTextureHandle = IntPtr.Zero;

        // If I am correct we get the texture that gets displayed in the window, so we can write to it using our GPU
        commadBuffer?.WaitAndAcquireSwapchainTexture(
            window!,
            out swapchainTextureHandle,
            out uint _,
            out uint _
        );

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = swapchainTextureHandle, // Use the acquired handle
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            clear_color = new FColor(0.0f, 0.0f, 0.0f, 1),
        };

        Span<SDL_GPUColorTargetInfo> colorTargets = [colorTargetInfo];

        using (var renderPass = commadBuffer?.BeginRenderPass(colorTargets))
        {
            renderPass?.BindGraphicsPipeline(fillPipeline!);

            // 1. Define your desired margin in pixels.
            //    Using float because SDL_GPUViewport uses floats.
            float margin = 100.0f;

            // Get the current window dimensions as floats
            float currentWidth = window!.Size.X;
            float currentHeight = window!.Size.Y;

            // 2. Calculate viewport dimensions.
            //    Subtract margin from left AND right (2 * margin) for width.
            //    Subtract margin from top AND bottom (2 * margin) for height.
            //    Use Math.Max to prevent negative width/height if the window is smaller than 2*margin.
            float viewW = Math.Max(0.0f, currentWidth - (2.0f * margin));
            float viewH = Math.Max(0.0f, currentHeight - (2.0f * margin));

            // 3. Calculate viewport top-left position.
            //    If the width is (totalWidth - 2*margin), starting at 'margin' centers it.
            //    Same logic applies vertically.
            float viewX = margin;
            float viewY = margin;

            // Here we define the rectangular area we want to drawn in.
            // Reminder that: " GPU API uses a left-handed coordinate system"
            // https://wiki.libsdl.org/SDL3/CategoryGPU#coordinate-system
            renderPass?.SetViewport(
                new()
                {
                    x = viewX,
                    y = viewY,
                    w = viewW,
                    h = viewH,
                }
            );
            renderPass?.DrawPrimitives(3, 1, 0, 0);
        }
        commadBuffer?.Submit();
    }
}
