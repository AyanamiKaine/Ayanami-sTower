using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;

namespace ShaderUniformBuffer;

/// <summary>
/// Here we are showing how to upload data to the gpu using
/// the uniform vertex buffer. This is data that can be used
/// by shaders as input for the calculations.
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
    private static GpuGraphicsPipeline? fillPipeline = null;
    private static GpuCommandBuffer? commadBuffer = null;
    private static bool _shouldQuit = false;
    private static GpuBuffer? vertexBuffer = null;
    private static PerFrameConstants vertexUniformBufferdata;

    private record struct PerFrameConstants(uint frameCount);

    private static unsafe void Main()
    {
        Init();
        while (!_shouldQuit)
        {
            vertexUniformBufferdata.frameCount = (uint)SDL_GetTicks();

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

    private static unsafe void Init()
    {
        SdlHost.Init(SdlSubSystem.Everything);
        window = new Window("Shader Uniform Buffer Example", 400, 400, WindowFlags.Resizable);
        gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true,
            null
        );
        gpuDevice.ClaimWindow(window);

        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");
        vertexUniformBufferdata = new();

        GpuShader vertexShader = GpuShader.CompileAndLoadHLSLToSPIRV(
            gpuDevice,
            "RawTriangle.vert.hlsl"
        );
        GpuShader fragmentShader = GpuShader.CompileAndLoadHLSLToSPIRV(
            gpuDevice,
            "SolidColor.frag.hlsl"
        );

        uint samplerCount = vertexShader.Metadata.num_samplers;
        uint uniformBufferCount = vertexShader.Metadata.num_uniform_buffers;

        Console.WriteLine($"Shader created with {samplerCount} samplers.");
        // This should print 1 because we define and use one Constant buffer in
        // RawTriangle.vert.hlsl
        Console.WriteLine($"Shader created with {uniformBufferCount} uniformBufferCount.");

        // Create the pipeline
        //TODO: Implement an easy abstraction for doing the GraphicsPipeline creation
        //possible creating an builder pattern for it. This would make things much more easier.
        SDL_GPUColorTargetDescription* colorTargetDescriptions =
            stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0].format = gpuDevice.GetSwapchainTextureFormat(window);

        SDL_GPUVertexBufferDescription sDL_GPUVertexBufferDescription =
            new()
            {
                slot = 0,
                pitch = (uint)sizeof(PerFrameConstants),
                input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            };

        SDL_GPUVertexAttribute* attributes = stackalloc SDL_GPUVertexAttribute[1];
        attributes[0].location = 0;
        attributes[0].buffer_slot = 0;
        attributes[0].format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT;
        attributes[0].offset = 0;

        SDL_GPUVertexInputState vertexInputState =
            new()
            {
                // Point to null or keep the pointer but set count to 0
                vertex_buffer_descriptions = null, // Or &someDummyDesc if null disallowed
                num_vertex_buffers = 0,
                // Point to null or keep the pointer but set count to 0
                vertex_attributes = null, // Or &someDummyAttr if null disallowed
                num_vertex_attributes =
                    0 // Correctly state there are NO attributes
                ,
            };

        SDL_GPUGraphicsPipelineCreateInfo pipelineCreateInfo =
            new()
            {
                target_info = new()
                {
                    num_color_targets = 1,
                    color_target_descriptions = colorTargetDescriptions,
                },
                //vertex_input_state
                primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                vertex_shader = vertexShader.Handle,
                fragment_shader = fragmentShader.Handle,
                vertex_input_state = vertexInputState,
            };

        pipelineCreateInfo.rasterizer_state.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL;
        //This only shows the outline, comment out the line above and uncomment the line below
        //pipelineCreateInfo.rasterizer_state.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE;



        // Here we are defining one vertex buffer
        vertexBuffer = gpuDevice.CreateBuffer<PerFrameConstants>(GpuBufferUsageFlags.Vertex);

        fillPipeline = gpuDevice.CreateGraphicsPipeline(pipelineCreateInfo);
    }

    private static void Draw()
    {
        commadBuffer = gpuDevice?.AcquireCommandBuffer();
        commadBuffer?.PushVertexUniformData(0, ref vertexUniformBufferdata);

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
            renderPass?.BindVertexBuffer(0, vertexBuffer!);
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
