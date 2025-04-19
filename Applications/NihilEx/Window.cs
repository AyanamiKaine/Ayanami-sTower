using SDL3;
using System; // Added for IDisposable and GC

namespace NihilEx
{
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
    public class Window(
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
        bool isNotFocusable = false) : IDisposable
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
        /// Internal storage for the native SDL_Renderer pointer.
        /// </summary>
        /// <remarks>
        /// This field holds the handle to the underlying SDL renderer object associated with the window after successful initialization via <see cref="M:NihilEx.Window.Initialize"/>.
        /// It is marked <c>internal</c> and should generally not be accessed directly by consumers of the class.
        /// Its value is <see cref="F:System.IntPtr.Zero"/> before initialization or if initialization fails.
        /// </remarks>
        internal nint _renderer;

        /// <summary>
        /// Flag indicating whether the object's resources have been disposed.
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be fullscreen.
        /// </summary>
        public bool IsFullscreen { get; } = isFullscreen;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be hidden initially.
        /// </summary>
        public bool IsHidden { get; } = isHidden;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be borderless.
        /// </summary>
        public bool IsBorderless { get; } = isBorderless;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be resizable.
        /// </summary>
        public bool IsResizable { get; } = isResizable;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be modal.
        /// </summary>
        public bool IsModal { get; } = isModal;

        /// <summary>
        /// Gets a value indicating whether the window was requested with high-pixel-density support.
        /// </summary>
        public bool IsHighPixelDensity { get; } = isHighPixelDensity;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be always on top.
        /// </summary>
        public bool IsAlwaysOnTop { get; } = isAlwaysOnTop;

        /// <summary>
        /// Gets a value indicating whether the window was requested as a utility window.
        /// </summary>
        public bool IsUtility { get; } = isUtility;

        /// <summary>
        /// Gets a value indicating whether the window was requested as a tooltip.
        /// </summary>
        public bool IsTooltip { get; } = isTooltip;

        /// <summary>
        /// Gets a value indicating whether the window was requested as a popup menu.
        /// </summary>
        public bool IsPopupMenu { get; } = isPopupMenu;

        /// <summary>
        /// Gets a value indicating whether the window was requested with transparency support.
        /// </summary>
        public bool IsTransparent { get; } = isTransparent;

        /// <summary>
        /// Gets a value indicating whether the window was requested to be non-focusable.
        /// </summary>
        public bool IsNotFocusable { get; } = isNotFocusable;

        /// <summary>
        /// Gets the initial width of the window requested at creation.
        /// </summary>
        public int Width { get; } = width;

        /// <summary>
        /// Gets the initial height of the window requested at creation.
        /// </summary>
        public int Height { get; } = height;

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        public string Title { get; } = title;

        /// <summary>
        /// Initializes the necessary SDL subsystems and creates the actual SDL window and renderer.
        /// </summary>
        /// <returns><c>true</c> if initialization and resource creation were successful; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method **MUST** be called after constructing a <see cref="T:NihilEx.Window"/> object and before attempting any rendering or event polling.
        /// It initializes SDL subsystems (Video, Audio, Events, Sensor), combines the window flags based on the constructor parameters,
        /// and calls <c>SDL.CreateWindowAndRenderer</c>. Errors are logged via <c>SDL.LogError</c> if any step fails.
        /// </remarks>
        public bool Initialize()
        {
            // Initialize SDL subsystems
            if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Events | SDL.InitFlags.Sensor))
            {
                SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
                return false;
            }

            // Combine window flags based on properties
            SDL.WindowFlags flags = 0; // Start with no flags
            if (IsFullscreen) flags |= SDL.WindowFlags.Fullscreen;
            if (IsHidden) flags |= SDL.WindowFlags.Hidden;
            if (IsBorderless) flags |= SDL.WindowFlags.Borderless;
            if (IsResizable) flags |= SDL.WindowFlags.Resizable;
            if (IsModal) flags |= SDL.WindowFlags.Modal;
            if (IsHighPixelDensity) flags |= SDL.WindowFlags.HighPixelDensity;
            if (IsAlwaysOnTop) flags |= SDL.WindowFlags.AlwaysOnTop;
            if (IsUtility) flags |= SDL.WindowFlags.Utility;
            if (IsTooltip) flags |= SDL.WindowFlags.Tooltip;
            if (IsPopupMenu) flags |= SDL.WindowFlags.PopupMenu;
            if (IsTransparent) flags |= SDL.WindowFlags.Transparent;
            if (IsNotFocusable) flags |= SDL.WindowFlags.NotFocusable;

            // Create the SDL window and renderer
            if (!SDL.CreateWindowAndRenderer(Title, Width, Height, flags, out _window, out _renderer))
            {
                SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
                // Attempt to clean up SDL if window creation failed after init succeeded
                SDL.Quit();
                return false;
            }

            // Initialization successful
            return true;
        }

        /// <summary>
        /// Sets the color used for drawing operations (like clearing the renderer).
        /// </summary>
        /// <param name="r">The red component of the color (0-255).</param>
        /// <param name="g">The green component of the color (0-255).</param>
        /// <param name="b">The blue component of the color (0-255).</param>
        /// <param name="a">The alpha component (opacity) of the color (0=transparent, 255=opaque).</param>
        /// <remarks>Requires <see cref="M:NihilEx.Window.Initialize"/> to have been called successfully.</remarks>
        public void SetRenderDrawColor(byte r, byte g, byte b, byte a)
        {
            if (_renderer == IntPtr.Zero) return; // Safety check
            SDL.SetRenderDrawColor(_renderer, r, g, b, a);
        }

        /// <summary>
        /// Clears the entire rendering target (the window's backbuffer) with the currently set draw color.
        /// </summary>
        /// <remarks>
        /// This should typically be called at the beginning of each frame's rendering sequence.
        /// Requires <see cref="M:NihilEx.Window.Initialize"/> to have been called successfully.
        /// </remarks>
        public void RenderClear()
        {
            if (_renderer == IntPtr.Zero) return; // Safety check
            SDL.RenderClear(_renderer);
        }

        /// <summary>
        /// Updates the screen with the contents of the renderer's backbuffer, making drawing operations visible.
        /// </summary>
        /// <remarks>
        /// This should typically be called at the end of each frame's rendering sequence.
        /// Requires <see cref="M:NihilEx.Window.Initialize"/> to have been called successfully.
        /// </remarks>
        public void RenderPresent()
        {
            if (_renderer == IntPtr.Zero) return; // Safety check
            SDL.RenderPresent(_renderer);
        }

        /// <summary>
        /// Polls for currently pending SDL events (keyboard, mouse, window events, etc.).
        /// </summary>
        /// <param name="e">An <see cref="T:SDL3.SDL.Event"/> structure that will be filled with the event data if an event is pending.</param>
        /// <returns><c>true</c> if an event was pending and returned in the <paramref name="e"/> parameter; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method should be called repeatedly within the application's main loop to process events and keep the window responsive.
        /// Requires <see cref="M:NihilEx.Window.Initialize"/> to have been called successfully.
        /// </remarks>
        public bool PollEvent(out SDL.Event e)
        {
            // No need to check _window or _renderer here, SDL.PollEvent handles null internally gracefully (usually).
            // However, SDL must have been initialized. The Initialize method handles this.
            return SDL.PollEvent(out e);
        }

        /// <summary>
        /// Releases the unmanaged resources (SDL window and renderer) and potentially quits SDL.
        /// </summary>
        /// <param name="disposing">
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
                // Destroy renderer before window
                if (_renderer != IntPtr.Zero)
                {
                    SDL.DestroyRenderer(_renderer);
                    _renderer = IntPtr.Zero; // Mark as destroyed
                }
                if (_window != IntPtr.Zero)
                {
                    SDL.DestroyWindow(_window);
                    _window = IntPtr.Zero; // Mark as destroyed
                }

                // Quit SDL subsystems
                // WARNING: See remarks section about placing SDL.Quit() here.
                SDL.Quit();

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
}
