using System.Drawing;
using AyanamisTower.NihilEx.ECS;
using SDL3;

namespace AyanamisTower.NihilEx;
/// <summary>
/// RENDERER
/// </summary>
public class Renderer(nint window, string name = "") : IDisposable
{
    internal nint _renderer = SDL.SDL_CreateRenderer(window, name);
    /// <summary>
    /// Flag indicating whether the object's resources have been disposed.
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    /// Backing field for the DrawColor property.
    /// </summary>
    private RgbaColor _drawColor;

    /// <summary>
    /// Backing field for the VSync property.
    /// </summary>
    private bool _vSync;

    /// <summary>
    /// Gets or sets the color used for drawing operations (Rect, Line, Point).
    /// When set, updates the internal state and the SDL renderer's draw color.
    /// </summary>
    public RgbaColor DrawColor
    {
        get => _drawColor;
        set
        {
            if (_drawColor != value)
            {
                _drawColor = value;
                SDL.SDL_SetRenderDrawColor(_renderer,
                    r: value.R,
                    g: value.G,
                    b: value.B,
                    a: value.A);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether VSync is enabled for the renderer.
    /// When set, updates the internal state and the SDL renderer's VSync setting.
    /// </summary>
    public bool VSync
    {
        get => _vSync;
        set
        {
            // Avoid unnecessary SDL calls if the value hasn't changed
            if (_vSync != value)
            {
                _vSync = value;
                SDL.SDL_SetRenderVSync(_renderer, value ? 1 : 0);
            }
        }
    }

    /// <summary>
    /// Renders a line between two points using the current draw color.
    /// </summary>
    /// <param name="x1">The x-coordinate of the starting point.</param>
    /// <param name="y1">The y-coordinate of the starting point.</param>
    /// <param name="x2">The x-coordinate of the ending point.</param>
    /// <param name="y2">The y-coordinate of the ending point.</param>
    public void RenderLine(float x1, float y1, float x2, float y2)
    {
        SDL.SDL_RenderLine(_renderer, x1, y1, x2, y2);
    }

    /// <summary>
    /// Clears the current rendering target with the drawing color.
    /// </summary>
    public void Clear()
    {
        SDL.SDL_RenderClear(_renderer);
    }

    /// <summary>
    /// Updates the screen with any rendering performed since the previous call.
    /// </summary>
    public void Present()
    {
        SDL.SDL_RenderPresent(_renderer);
    }

    /// <summary>
    /// Renders debug text at the specified coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate to render the text at.</param>
    /// <param name="y">The y-coordinate to render the text at.</param>
    /// <param name="text">The text to render.</param>
    public void ShowDebugText(float x, float y, string text)
    {
        SDL.SDL_RenderDebugText(_renderer, x, y, text);
    }

    /// <summary>
    /// Releases the unmanaged resources (SDL window) and potentially quits SDL.        /// <param name="disposing">
    /// <c>true</c> if called directly or indirectly from user code (via <see cref="M:System.IDisposable.Dispose"/>);
    /// <c>false</c> if called from the finalizer.
    /// </param>
    /// <remarks>
    /// This method handles the actual cleanup. If <paramref name="disposing"/> is <c>true</c>, it can also release managed resources (though none are currently held directly by this class).
    /// It always attempts to destroy the SDL renderer and window if they were created.
    /// **Important:** The call to <c>SDL.Quit()</c> here assumes this <c>Window</c> instance is the sole user of SDL in the application.
    /// If other parts of your application use SDL independently, <c>SDL.Quit()</c> should be called separately at the very end of the application's lifetime,
    /// not within this specific window's disposal logic. Consider moving <c>SDL.Init</c> and <c>SDL.Quit</c> outside this class if managing multiple SDL components.
    /// </remarks>
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                // Currently none directly held by this class that need disposal here.
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            if (_renderer != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_renderer);
                _renderer = IntPtr.Zero; // Mark as destroyed
            }
            _isDisposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="T:NihilEx.Window"/> object.
    /// </summary>
    /// <remarks>
    /// Call <c>Dispose</c> when you are finished using the <see cref="T:NihilEx.Window"/>. The <c>Dispose</c> method leaves the <see cref="T:NihilEx.Window"/> in an unusable state.
    /// After calling <c>Dispose</c>, you must release all references to the <see cref="T:NihilEx.Window"/> so the garbage collector can reclaim the memory that the <see cref="T:NihilEx.Window"/> was occupying.
    /// Prefer using a <c>using</c> statement for automatic disposal.
    /// </remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this); // Prevent finalizer from running since cleanup is done
    }

    /// <summary>
    /// Finalizer that acts as a safeguard to release unmanaged resources if <see cref="M:NihilEx.Window.Dispose"/> was not called.
    /// </summary>
    /// <remarks>
    /// This provides a fallback mechanism. However, relying on finalizers for cleanup is generally discouraged due to performance implications and non-deterministic timing.
    /// Always prefer explicit disposal using <see cref="M:NihilEx.Window.Dispose"/> or a <c>using</c> statement.
    /// </remarks>
    ~Renderer()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }
}