using System;

namespace AyanamisTower.NihilEx.SDLWrapper.Tests;

#pragma warning disable CS0414 // Missing XML comment for publicly visible type or member


/// <summary>
/// Here we want to test your graphics pipeline builder so it works as expected
/// </summary>
public class GraphicsPipelineBuilderTests : IDisposable
{
    private bool _sdlInitialized = false;
    private Window? _windowUnderTest = null; // Field to hold the window for disposal
    private static GpuDevice? _gpuDevice = null;
    private static GpuGraphicsPipeline? _fillPipeline = null;
    private static GpuCommandBuffer? _commadBuffer = null;

    /// <summary>
    /// Constructor runs before each test fact in this class.
    /// Initializes SDL subsystems needed for the tests.
    /// </summary>
    public GraphicsPipelineBuilderTests()
    {
        try
        {
            // Initialize SDL Video and Events subsystems, essential for window creation and basic interaction.
            SdlHost.Init(SdlSubSystem.Video | SdlSubSystem.Events);
            _sdlInitialized = true;
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
    /// Dispose runs after each test fact in this class.
    /// Cleans up created resources and quits SDL.
    /// </summary>
    public void Dispose()
    {
        // Dispose the window if it was created during a test
        _windowUnderTest?.Dispose();
        _windowUnderTest = null;

        _gpuDevice?.Dispose();
        _fillPipeline?.Dispose();

        // Quit SDL if it was successfully initialized
        if (_sdlInitialized)
        {
            SdlHost.Quit();
            _sdlInitialized = false; // Reset flag
        }
        GC.SuppressFinalize(this); // Standard practice for IDisposable
    }
}
