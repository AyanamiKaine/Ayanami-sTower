using System.Runtime.InteropServices;
using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;

namespace MeshExample;

/// <summary>
/// This is a port of https://github.com/TheSpydog/SDL_gpu_examples/blob/main/Examples/BasicTriangle.c
/// using my SDL3 wrapper around the native SDL3 bindings.
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
    private static GpuGraphicsPipeline? fillPipeline = null;
    private static GpuCommandBuffer? commadBuffer = null;
    private static bool _shouldQuit = false;
    private static GpuMeshBuffers? mesh = null; // To hold vertex/index buffers

    // --- Mesh Data ---
    //Rectangle
    private static readonly Vertex[] vertices =
    [
        // Position             Color                           TexCoord (unused)
        new(new FPoint(-0.5f, -0.5f), new FColor(1.0f, 0.0f, 0.0f, 1.0f), new FPoint(0, 1)), // Bottom-left, Red
        new(new FPoint(0.5f, -0.5f), new FColor(0.0f, 1.0f, 0.0f, 1.0f), new FPoint(1, 1)), // Bottom-right, Green
        new(new FPoint(0.5f, 0.5f), new FColor(0.0f, 0.0f, 1.0f, 1.0f), new FPoint(1, 0)), // Top-right, Blue
        new(
            new FPoint(-0.5f, 0.5f),
            new FColor(1.0f, 1.0f, 0.0f, 1.0f),
            new FPoint(0, 0)
        ) // Top-left, Yellow
        ,
    ];

    private static readonly uint[] indices =
    [
        0,
        1,
        2, // First triangle (Bottom-left -> Bottom-right -> Top-right)
        0,
        2,
        3 // Second triangle (Bottom-left -> Top-right -> Top-left)
        ,
    ];

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

    private static unsafe void Init()
    {
        SdlHost.Init(SdlSubSystem.Everything);
        window = new Window("Basic Rectangle Example", 400, 400, WindowFlags.Resizable);
        gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true
        );
        gpuDevice.ClaimWindow(window);

        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");
        Console.WriteLine("Uploading Mesh Data...");
        // Use the wrapper's Vertex struct type here
        mesh = gpuDevice.UploadMesh<Vertex>(indices.AsSpan(), vertices.AsSpan());
        Console.WriteLine(
            $"Uploaded Mesh: VB={mesh.VertexBuffer.Handle}, IB={mesh.IndexBuffer.Handle}, Indices={mesh.IndexCount}"
        );

        GpuShader vertexShader = GpuShader.CompileAndLoadHLSLToSPIRV(
            gpuDevice,
            "Rectangle.vert.hlsl"
        );

        GpuShader fragmentShader = GpuShader.LoadShader(gpuDevice, "Shader/SolidColor.frag");

        // --- Define Vertex Input State ---

        // Describe each attribute within the Vertex struct
        SDL_GPUVertexAttribute* vertexAttributes = stackalloc SDL_GPUVertexAttribute[3];
        // Position (assuming FPoint is two floats) - Location 0
        vertexAttributes[0] = new()
        {
            location = 0,
            buffer_slot = 0, // Uses the buffer bound to vertexBinding.binding (slot 0)
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position)),
        };
        // Color (assuming FColor is four floats) - Location 1
        vertexAttributes[1] = new()
        {
            location = 1,
            buffer_slot = 0, // Uses the buffer bound to vertexBinding.binding (slot 0)
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Color)),
        };
        // TexCoord (assuming FPoint is two floats) - Location 2
        vertexAttributes[2] = new()
        {
            location = 2,
            buffer_slot = 0, // Uses the buffer bound to vertexBinding.binding (slot 0)
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)),
        };

        // --- Define Color Target State ---
        SDL_GPUColorTargetDescription colorTargetDescription =
            new() { format = gpuDevice.GetSwapchainTextureFormat(window) };

        SDL_GPUVertexBufferDescription vertexBufferDesc =
            new() // Use the new struct name
            {
                slot = 0, // Use the new field name 'slot'
                input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                pitch = (uint)sizeof(Vertex),
                instance_step_rate = 0,
            };

        // Create the pipeline
        //TODO: Implement an easy abstraction for doing the GraphicsPipeline creation
        //possible creating an builder pattern for it. This would make things much more easier.
        SDL_GPUColorTargetDescription* colorTargetDescriptions =
            stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0].format = gpuDevice.GetSwapchainTextureFormat(window);

        SDL_GPUGraphicsPipelineCreateInfo pipelineCreateInfo =
            new()
            {
                vertex_shader = vertexShader.Handle,
                fragment_shader = fragmentShader.Handle,
                // Assign the vertex input state
                vertex_input_state = new()
                {
                    vertex_attributes = vertexAttributes,
                    num_vertex_attributes = 3,
                    vertex_buffer_descriptions = &vertexBufferDesc,
                    num_vertex_buffers = 1,
                },
                primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                // Rasterizer defaults are often okay (CullMode=None, FillMode=Fill, FrontFace=CCW)
                rasterizer_state = new()
                {
                    fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                    cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                    front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE,
                },
                // Multisample defaults are okay (1x sample count)
                multisample_state = new()
                {
                    sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
                },
                // Depth/Stencil defaults (disabled) are okay for this example
                depth_stencil_state = new()
                {
                    enable_depth_test = false,
                    enable_depth_write = false,
                },
                // Target Info
                target_info = new()
                {
                    num_color_targets = 1,
                    color_target_descriptions = &colorTargetDescription,
                },
                // props = 0 // No extra properties needed for now
            };

        pipelineCreateInfo.rasterizer_state.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL;
        //This only shows the outline, comment out the line above and uncomment the line below
        //pipelineCreateInfo.rasterizer_state.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE;
        fillPipeline = gpuDevice.CreateGraphicsPipeline(pipelineCreateInfo);
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
            renderPass?.BindVertexBuffer(0, mesh!.VertexBuffer); // Bind VB to slot 0
            renderPass?.BindIndexBuffer(mesh!.IndexBuffer, 0, mesh.IndexElementSize); // Bind IB

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
            renderPass?.DrawIndexedPrimitives(mesh!.IndexCount, 1, 0, 0, 0);
        }
        commadBuffer?.Submit();
    }
}
