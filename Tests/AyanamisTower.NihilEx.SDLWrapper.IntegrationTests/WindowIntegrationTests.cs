namespace AyanamisTower.NihilEx.SDLWrapper.Tests;

/// <summary>
/// Integration tests that actually invoke the underlying native SDL functions.
/// This class handles SDL Init/Quit for tests within it.
/// </summary>
public class IntegrationTests : IDisposable // Implement IDisposable for cleanup
{
    private bool _sdlInitialized = false;
    private Window? _windowUnderTest = null; // Field to hold the window for disposal

    /// <summary>
    /// Constructor runs before each test fact in this class.
    /// Initializes SDL subsystems needed for the tests.
    /// </summary>
    public IntegrationTests()
    {
        try
        {
            // Initialize SDL Video and Events subsystems, essential for window creation and basic interaction.
            SdlHost.Init(SdlSubSystem.Video | SdlSubSystem.Events);
            _sdlInitialized = true;

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
    /// Dispose runs after each test fact in this class.
    /// Cleans up created resources and quits SDL.
    /// </summary>
    public void Dispose()
    {
        // Dispose the window if it was created during a test
        _windowUnderTest?.Dispose();
        _windowUnderTest = null;

        // Quit SDL if it was successfully initialized
        if (_sdlInitialized)
        {
            SdlHost.Quit();
            _sdlInitialized = false; // Reset flag
        }
        GC.SuppressFinalize(this); // Standard practice for IDisposable
    }

    /// <summary>
    /// Tests creating a basic, hidden SDL window using the wrapper.
    /// </summary>
    [Fact]
    public void WindowCreationCreatesValidWindowHandle()
    {
        // Arrange
        const string testTitle = "xUnit Test Window";
        const int testWidth = 320;
        const int testHeight = 240;
        // Use Hidden flag to avoid visible windows flashing during tests
        const WindowFlags testFlags = WindowFlags.Hidden;
        try
        {
            // Act
            // Create the window using the wrapper class constructor
            Window? createdWindow = new(testTitle, testWidth, testHeight, testFlags);
            _windowUnderTest = createdWindow; // Assign to field for cleanup in Dispose

            // Assert
            // 1. Check if the window object was created
            Assert.NotNull(createdWindow);

            // 2. Check if the native handle is valid (not Zero)
            Assert.NotEqual(IntPtr.Zero, createdWindow.Handle);

            // 3. Check if the disposed flag is initially false
            Assert.False(createdWindow.IsDisposed);

            // 4. (Optional) Check if properties reflect creation parameters (Title might be exact)
            // Note: Size might be adjusted by WM even if hidden, query it but maybe don't assert exact match unless necessary.
            Assert.Equal(testTitle, createdWindow.Title);
            var currentSize = createdWindow.Size;
            // Assert.Equal(testWidth, currentSize.X); // Be cautious with size assertions
            // Assert.Equal(testHeight, currentSize.Y);

            // 5. (Optional) Check if a valid ID is assigned
            Assert.NotEqual(0u, createdWindow.Id); // Window IDs should be non-zero
        }
        catch (SDLException ex)
        {
            // Fail the test clearly if an SDL operation fails
            Assert.Fail(
                $"SDL operation failed during test: {ex.Message} - SDL Error: {SdlHost.GetError()}"
            );
        }
        catch (Exception ex) // Catch unexpected exceptions
        {
            Assert.Fail($"Unexpected exception during test: {ex}");
        }
        // No finally block needed for cleanup here, IDisposable handles it.
    }

    /// <summary>
    /// Setting the position property of a window should update the windows
    /// position using the native sdl functions.
    /// </summary>
    [Fact]
    public void WindowSetPositionUpdatesPositionProperty()
    {
        // Arrange
        const int newX = 50,
            newY = 60;
        const int width = 100,
            height = 100;

        try
        {
            // Use SDL_WINDOWPOS_CENTERED or specific coords initially if needed
            _windowUnderTest = new Window(
                "Pos Test",
                width,
                height,
                WindowFlags.Hidden | WindowFlags.Borderless
            ); // Borderless might interfere less with positioning
            Assert.NotNull(_windowUnderTest);

            // Act - Set Position
            _windowUnderTest.Position = new Point(newX, newY);

            // Read back position
            Point finalPosition = _windowUnderTest.Position;

            // Assert
            // Note: Window manager might still adjust position slightly.
            // A tolerance might be needed, or assert it's different from start & close to target.
            Assert.Equal(newX, finalPosition.X);
            Assert.Equal(newY, finalPosition.Y);
        }
        catch (SDLException ex)
        {
            Assert.Fail(
                $"SDL operation failed during test: {ex.Message} - SDL Error: {SdlHost.GetError()}"
            );
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception during test: {ex}");
        }
    }
}
