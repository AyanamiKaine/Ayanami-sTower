using SDL3;
using System; // Added for IDisposable and GC

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents and manages an SDL3 window and its associated renderer.
/// This class serves as a high-level wrapper around SDL3's windowing and basic rendering functionalities,
/// simplifying setup, event handling, and resource management.
/// </summary>
/// <remarks>
/// <para>
/// This class simplifies the process of creating, managing, and interacting with an application window using the SDL3 library.
/// It handles the initialization of required SDL subsystems, creates both an SDL_Window and an SDL_Renderer,
/// provides methods for basic rendering operations (clearing, presenting), and allows polling for SDL events.
/// </para>
/// <para>
/// Crucially, this class implements <see cref="T:System.IDisposable"/>. It manages unmanaged SDL resources (the window and renderer handles)
/// and ensures SDL subsystems are properly shut down. You **must** dispose of <c>Window</c> instances when they are no longer needed
/// to prevent resource leaks. The recommended way to do this is with a <c>using</c> statement.
/// </para>
/// <para>
/// After constructing a <see cref="T:NihilEx.Window"/> object, you **must** call the <see cref="M:NihilEx.Window.Initialize"/> method
/// to actually create the underlying SDL window and renderer before performing any rendering or event polling operations.
/// </para>
/// <para>
/// Dependencies: This class requires the native SDL3 library and appropriate C# bindings (like SDL3-cs) to be available.
/// </para>
/// </remarks>
/// <example>
/// The following example shows basic usage: creating a window, initializing it, running a simple event loop,
/// and ensuring proper disposal with a `using` statement.
/// <code><![CDATA[
/// using SDL3;
/// using NihilEx; // Assuming Window is in this namespace
/// using System;
///
/// public class Game
/// {
///     public static void Main(string[] args)
///     {
///         // Create the window configuration
///         using (Window window = new Window("My SDL3 Window", 800, 600, isResizable: true))
///         {
///             // *** IMPORTANT: Initialize the window ***
///             if (!window.Initialize())
///             {
///                 Console.WriteLine("Failed to initialize window!");
///                 return;
///             }
///
///             bool quit = false;
///             SDL.Event e;
///
///             // Main loop
///             while (!quit)
///             {
///                 // Handle events
///                 while (window.PollEvent(out e))
///                 {
///                     if (e.type == SDL.EventType.Quit)
///                     {
///                         quit = true;
///                     }
///                     // Add other event handling (keyboard, mouse, etc.) here
///                 }
///
///                 // Rendering
///                 window.SetRenderDrawColor(0, 0, 255, 255); // Blue background
///                 window.RenderClear();
///
///                 // Add drawing code here...
///
///                 window.RenderPresent();
///             }
///         } // Window.Dispose() is automatically called here by the 'using' statement
///     }
/// }
/// ]]></code>
/// </example>
public class Window : IDisposable
{
    /// <summary>
    /// Internal storage for the native SDL_Window pointer.
    /// </summary>
    /// <remarks>
    /// This field holds the handle to the underlying SDL window object after successful initialization via <see cref="M:NihilEx.Window.Initialize"/>.
    /// It is marked <c>internal</c> and should generally not be accessed directly by consumers of the class.
    /// Its value is <see cref="F:System.IntPtr.Zero"/> before initialization or if initialization fails.
    /// </remarks>
    internal nint _window;

    /// <summary>
    /// Flag indicating whether the object's resources have been disposed.
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be fullscreen.
    /// </summary>
    public bool IsFullscreen
    {
        get
        {
            if (_window == IntPtr.Zero)
                throw new Exception("Window is null");
            return SDL.GetWindowFlags(_window).HasFlag(SDL.WindowFlags.Fullscreen);
        }
        set
        {
            if (_window == IntPtr.Zero)
                throw new Exception("Window is null");
            SDL.SetWindowFullscreen(_window, value);
        }
    }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be hidden initially.
    /// </summary>
    public bool IsHidden { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be borderless.
    /// </summary>
    public bool IsBorderless { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be resizable.
    /// </summary>
    public bool IsResizable { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be modal.
    /// </summary>
    public bool IsModal { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested with high-pixel-density support.
    /// </summary>
    public bool IsHighPixelDensity { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be always on top.
    /// </summary>
    public bool IsAlwaysOnTop { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested as a utility window.
    /// </summary>
    public bool IsUtility { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested as a tooltip.
    /// </summary>
    public bool IsTooltip { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested as a popup menu.
    /// </summary>
    public bool IsPopupMenu { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested with transparency support.
    /// </summary>
    public bool IsTransparent { get; }

    /// <summary>
    /// Gets or Sets a value indicating whether the window was requested to be non-focusable.
    /// </summary>
    public bool IsNotFocusable { get; }

    /// <summary>
    /// Gets or Sets the current width of the window.
    /// </summary>
    public int Width
    {
        get
        {
            if (_window == IntPtr.Zero)
                throw new Exception("Window is null");
            SDL.GetWindowSize(_window, out int w, out _);
            return w;
        }
        set
        {
            if (_window != IntPtr.Zero)
                SDL.SetWindowSize(_window, value, Width);
        }
    }

    /// <summary>
    /// Gets or Sets the current height of the window.
    /// </summary>
    public int Height
    {
        get
        {
            if (_window == IntPtr.Zero)
                throw new Exception("Window is null");
            SDL.GetWindowSize(_window, out _, out int h);
            return h;
        }
        set
        {
            if (_window != IntPtr.Zero)
                SDL.SetWindowSize(_window, Width, value);
        }
    }

    /// <summary>
    /// Gets or Sets the title of the window.
    /// </summary>
    public string Title
    {
        get
        {
            if (_window == IntPtr.Zero)
                throw new Exception("Window is null");
            return SDL.GetWindowTitle(_window);
        }
        set
        {
            if (_window != IntPtr.Zero)
                SDL.SetWindowTitle(_window, value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class and creates the underlying SDL window.
    /// </summary>
    /// <param name="title">The text to display in the window's title bar.</param>
    /// <param name="width">The initial width of the window's client area, in screen coordinates.</param>
    /// <param name="height">The initial height of the window's client area, in screen coordinates.</param>
    /// <param name="isBorderless">If <c>true</c>, creates a window without standard decorations (title bar, borders).</param>
    /// <param name="isResizable">If <c>true</c>, allows the user to resize the window.</param>
    /// <param name="isFullscreen">If <c>true</c>, attempts to create a fullscreen window.</param>
    /// <param name="isHidden">If <c>true</c>, creates the window initially hidden.</param>
    /// <param name="isModal">If <c>true</c>, creates a modal window (intended to block input to other windows).</param>
    /// <param name="isHighPixelDensity">If <c>true</c>, requests a window suitable for high-DPI displays.</param>
    /// <param name="isAlwaysOnTop">If <c>true</c>, attempts to keep the window above other non-topmost windows.</param>
    /// <param name="isUtility">If <c>true</c>, hints that the window is a utility window (may affect appearance or behavior).</param>
    /// <param name="isTooltip">If <c>true</c>, hints that the window is a tooltip (may affect appearance or behavior).</param>
    /// <param name="isPopupMenu">If <c>true</c>, hints that the window is a popup menu (may affect appearance or behavior).</param>
    /// <param name="isTransparent">If <c>true</c>, requests a window with transparency support (compositor dependent).</param>
    /// <param name="isNotFocusable">If <c>true</c>, creates a window that cannot receive keyboard focus.</param>
    /// <remarks>
    /// This constructor initializes necessary SDL subsystems (Video, Audio, Events, Sensor) if they haven't been initialized already.
    /// It then attempts to create an SDL_Window with the specified parameters and flags.
    /// If window creation fails, an error is logged via <c>SDL.LogError</c>, but the constructor does not throw an exception directly.
    /// The internal window handle (<see cref="_window"/>) will be <see cref="IntPtr.Zero"/> in case of failure.
    /// **Important:** Consider moving SDL initialization (<c>SDL.Init</c>) and shutdown (<c>SDL.Quit</c>) outside this class
    /// if your application manages multiple SDL windows or other SDL components to avoid conflicts and ensure proper lifecycle management.
    /// </remarks>
    public Window(
    string title,
    int width,
    int height,
    bool isBorderless = false,
    bool isResizable = true,
    bool isFullscreen = false,
    bool isHidden = false,
    bool isModal = false,
    bool isHighPixelDensity = false,
    bool isAlwaysOnTop = false,
    bool isUtility = false,
    bool isTooltip = false,
    bool isPopupMenu = false,
    bool isTransparent = false,
    bool isNotFocusable = false)
    {
        // Initialize SDL subsystems
        // Consider moving SDL.Init and SDL.Quit outside this class if managing multiple SDL components
        // or if you need finer control over subsystem initialization.
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Events | SDL.InitFlags.Sensor))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
        }

        // Combine window flags based on properties
        SDL.WindowFlags flags = 0; // Start with no flags
        if (isFullscreen) flags |= SDL.WindowFlags.Fullscreen;
        if (isHidden) flags |= SDL.WindowFlags.Hidden;
        if (isBorderless) flags |= SDL.WindowFlags.Borderless;
        if (isResizable) flags |= SDL.WindowFlags.Resizable;
        if (isModal) flags |= SDL.WindowFlags.Modal;
        if (isHighPixelDensity) flags |= SDL.WindowFlags.HighPixelDensity;
        if (isAlwaysOnTop) flags |= SDL.WindowFlags.AlwaysOnTop;
        if (isUtility) flags |= SDL.WindowFlags.Utility;
        if (isTooltip) flags |= SDL.WindowFlags.Tooltip;
        if (isPopupMenu) flags |= SDL.WindowFlags.PopupMenu;
        if (isTransparent) flags |= SDL.WindowFlags.Transparent;
        if (isNotFocusable) flags |= SDL.WindowFlags.NotFocusable;

        // Create the SDL window
        _window = SDL.CreateWindow(title, width, height, flags);
        if (_window == IntPtr.Zero) // Check against IntPtr.Zero
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window: {SDL.GetError()}");
        }
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
            if (_window != IntPtr.Zero)
            {
                SDL.DestroyWindow(_window);
                _window = IntPtr.Zero; // Mark as destroyed
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
    ~Window()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }
}

