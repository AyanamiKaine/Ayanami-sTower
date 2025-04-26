using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;

namespace ClearScreen;

/// <summary>
/// This is a port of https://github.com/TheSpydog/SDL_gpu_examples/blob/main/Examples/ClearScreen.c
/// using my SDL3 wrapper around the native SDL3 bindings.
/// </summary>
public static class Program
{
    private static Window? window = null;
    private static GpuDevice? gpuDevice = null;
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
        window = new Window("Clear Screen Example", 600, 800, WindowFlags.Resizable);
        gpuDevice = new GpuDevice(GpuShaderFormat.SpirV, enableDebugMode: true);

        Console.WriteLine($"Created GPU device with driver: {gpuDevice.DriverName}");

        gpuDevice.ClaimWindow(window);
    }

    private static void Draw()
    {
        commadBuffer = gpuDevice?.AcquireCommandBuffer();
        IntPtr swapchainTextureHandle = IntPtr.Zero;
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
            clear_color = FColor.Red,
        };

        Span<SDL_GPUColorTargetInfo> colorTargets = [colorTargetInfo];

        using (var renderPass = commadBuffer?.BeginRenderPass(colorTargets)) { }
        commadBuffer?.Submit();
    }
}
