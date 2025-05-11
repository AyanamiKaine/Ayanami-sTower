using System;

namespace AyanamisTower.NihilEx.SDLWrapper.Tests;

/// <summary>
/// Here we want to tests notably loading and compiling shaders at runtime
/// </summary>
public class ShaderTests
{
    private string VertexShaderPath = "RawTriangle.vert.hlsl";

    /// <summary>
    /// Constructor runs before each test fact in this class.
    /// Initializes SDL subsystems needed for the tests.
    /// </summary>
    public ShaderTests()
    {
        try
        {
            // Initialize SDL Video and Events subsystems, essential for window creation and basic interaction.
            SdlHost.Init(SdlSubSystem.Everything);

            // Optional: Set headless driver hint if running in CI without a display.
            // Check SDL3 documentation for appropriate hint values ("dummy", "offscreen", etc.)
            // SDL_SetHint(SDL_HINT_VIDEODRIVER, "dummy");
        }
        catch (SDLException ex)
        {
            // Use Assert.Fail or throw an exception that xUnit understands to fail the test setup.
            throw new InvalidOperationException(
                $"Setup Failure: SDL Initialization failed: {ex.Message} - SDL Error: {SdlHost.GetError()}",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Setup Failure: Non-SDL exception during Init: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// We should be able to compile shaders using a simple method.
    /// </summary>
    [Fact]
    public void CompileShader()
    {
        var gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true
        );

        //GpuShader.CompileShader(VertexShaderPath, "./");
        _ = GpuShader.LoadShader(gpuDevice, VertexShaderPath);
    }

    /// <summary>
    /// We should be able to compile and load shaders using a simple method.
    /// </summary>
    [Fact]
    public void CompileAndLoadShaderSPIRV()
    {
        const string testTitle = "CompileAndLoadShaderSPIRV xUnit Test Window";
        const int testWidth = 320;
        const int testHeight = 240;
        // Use Hidden flag to avoid visible windows flashing during tests
        const WindowFlags testFlags = WindowFlags.Hidden;
        // Act
        // Create the window using the wrapper class constructor
        Window? createdWindow = new(testTitle, testWidth, testHeight, testFlags);

        var gpuDevice = new GpuDevice(GpuShaderFormat.SpirV, enableDebugMode: true);

        _ = GpuShader.CompileAndLoadSPIRVShader(gpuDevice, VertexShaderPath, enableDebug: true);
    }

    /// <summary>
    /// We should be able to load shaders using a simple method.
    /// </summary>
    [Fact]
    public void LoadShader()
    {
        var gpuDevice = new GpuDevice(
            GpuShaderFormat.SpirV | GpuShaderFormat.Msl | GpuShaderFormat.Dxil,
            enableDebugMode: true
        );
        _ = GpuShader.LoadShader(gpuDevice, VertexShaderPath);
    }
}
