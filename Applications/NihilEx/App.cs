using AyanamisTower.NihilEx.ECS;
using AyanamisTower.NihilEx.ECS.Events;
using AyanamisTower.NihilEx.SDLWrapper; // Assuming SdlHost, SdlEventArgs etc. are here
using Flecs.NET.Core;
using SDL3; // Keep for SDL enums like Keycode if needed by user code or ECS events

namespace AyanamisTower.NihilEx
{
    /// <summary>
    /// Base class for creating SDL applications using the SdlHost.RunApplication callback mechanism.
    /// Inherit from this class and override the OnInit, OnUpdate, and OnQuit methods.
    /// Event handling is managed internally and emits ECS events via the AppEntity.
    /// </summary>
    public abstract class App
    {
        private bool _shouldQuit = false; // Flag to signal exit request
        private Entity _appEntity;

        /// <summary>
        /// Gets the Flecs Entity associated with this application instance.
        /// </summary>
        /// <remarks>
        /// Use this entity to register observers for events emitted by the application's
        /// HandleEvent method (e.g., WindowResize, KeyDownEvent).
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// // Inside your OnInit method or later:
        /// AppEntity.Observe<WindowResize>(() =>
        /// {
        ///     Console.WriteLine($"Window resize observed via AppEntity!");
        /// });
        /// AppEntity.Observe((ref KeyDownEvent evt) => // Use 'ref' for struct events
        /// {
        ///     if(evt.Keycode == SDL.SDL_Keycode.SDLK_SPACE)
        ///         Console.WriteLine("Spacebar down!");
        /// });
        /// ]]></code>
        /// </example>
        /// <value>The application's root <see cref="Entity"/>.</value>
        public Entity AppEntity => _appEntity; // Use expression body for brevity

        // Instance of our DeltaTime class
        private readonly DeltaTime _deltaTimeManager = new();

        /// <summary>
        /// Gets the SDL Renderer associated with this application instance.
        /// This is created during the Initialize phase.
        /// </summary>
        protected Renderer? Renderer { get; private set; }
        /// <summary>
        /// Gets the SDL Window associated with this application instance.
        /// This is created during the Initialize phase.
        /// </summary>
        protected Window? Window { get; private set; }

        /// <summary>
        /// Gets or sets the current height of the application window.
        /// </summary>
        public int Height
        {
            get => Window?.Size.Y ?? 0;
            set
            {
                if (Window != null) Window.Size = new(Width, value);
            }
        }
        /// <summary>
        /// Gets or sets the current width of the application window.
        /// </summary>
        public int Width
        {
            get => Window?.Size.X ?? 0;
            set
            {
                if (Window != null) Window.Size = new(value, Height);
            }
        }
        /// <summary>
        /// Gets or sets the current title of the application window.
        /// </summary>
        public string Title
        {
            get => Window?.Title ?? "";
            set
            {
                if (Window != null) Window.Title = value;
            }
        }

        /// <summary>
        /// The initial height of the application window.
        /// </summary>
        public required int InitalHeight { init; get; }
        /// <summary>
        /// The initial width of the application window.
        /// </summary>
        public required int InitalWidth { init; get; }
        /// <summary>
        /// The initial title of the application window.
        /// </summary>
        public required string InitalTitle { init; get; }

        /// <summary>
        /// Gets the Flecs ECS World associated with this application.
        /// </summary>
        public World World { get; } = World.Create(); // Initialize Flecs World

        /// <summary>
        /// Gets the Engine instance associated with this application.
        /// </summary>
        public Engine? Engine { get; private set; }

        /// <summary>
        /// Runs the SDL application using the SdlHost callback mechanism.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        /// <returns>The exit code of the application.</returns>
        public int Run(string[] args)
        {
            Console.WriteLine("Starting SDL Application via SdlHost.RunApplication...");

            // SdlHost.RunApplication handles SDL_Init, SDL_Quit, and the event/update loop.
            int exitCode = SdlHost.RunApplication(
                Initialize, // Pass instance method directly
                Update,     // Pass instance method directly
                HandleEvent,// Pass instance method directly
                Cleanup,    // Pass instance method directly
                args        // Pass command line args
            );

            Console.WriteLine($"Application finished with exit code: {exitCode}");
            return exitCode;
        }

        // --- Instance methods matching SdlHost.RunApplication delegates ---

        /// <summary>
        /// Core initialization logic called by SdlHost.RunApplication.
        /// Creates Window, Renderer, Engine, ECS World, DeltaTime, etc.
        /// Calls the user-defined OnInit method.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>True to continue, False to exit immediately.</returns>
        private bool Initialize(string[] args)
        {
            Console.WriteLine($"App.Initialize: Thread {Environment.CurrentManagedThreadId}");
            try
            {
                // SdlHost.RunApplication handles SDL.Init based on its internal needs.

                // Initialize DeltaTime manager
                _deltaTimeManager.Initialize();

                // Create Engine and configure World
                Engine = new(World);
                //World.SetThreads(SdlHost.GetNumLogicalCores()); // Use SdlHost or SDL directly if available

                // Set singletons in the ECS world
                World.Set(_deltaTimeManager); // Make DeltaTime accessible via ECS

                // Create Window and Renderer using the wrapper's classes
                Window = new Window(InitalTitle, InitalWidth, InitalHeight, WindowFlags.Resizable); // Assuming WindowFlags enum exists
                Console.WriteLine($"Window created with ID: {Window.Id}");
                if (Window == null) throw new InvalidOperationException("Failed to create SDL Window.");
                World.Set(Window); // Add Window as singleton resource

                Renderer = Window.CreateRenderer();
                Console.WriteLine($"Renderer created: {Renderer.Name}");
                if (Renderer == null) throw new InvalidOperationException("Failed to create SDL Renderer.");
                Renderer.DrawColor = new SDLWrapper.Color(255, 255, 255, 255); // White (using wrapper's Color)
                World.Set(Renderer); // Add Renderer as singleton resource


                // Create the application's root entity for events
                _appEntity = World.Entity($"App: {Title}"); // Use property, will fetch from Window

                // Call user's initialization code
                if (!OnInit(args))
                {
                    Console.Error.WriteLine("Application OnInit returned false. Shutting down.");
                    _shouldQuit = true; // Signal immediate quit if OnInit fails
                    return false;
                }

                Console.WriteLine("App.Initialize successful.");
                return !_shouldQuit; // Return true to continue
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error during App.Initialize: {ex}");
                Console.ResetColor();
                _shouldQuit = true; // Ensure we signal quit on error
                return false; // Return false on failure
            }
        }

        /// <summary>
        /// Core update logic called repeatedly by SdlHost.RunApplication.
        /// Calculates delta time, calls user OnUpdate, progresses ECS world, handles rendering.
        /// </summary>
        /// <returns>True to continue, False to request exit.</returns>
        private bool Update()
        {
            if (_shouldQuit) return false; // Exit early if quit was requested

            try
            {
                // Calculate Delta Time
                _deltaTimeManager.Update();
                float deltaTime = _deltaTimeManager.DeltaSeconds;

                // Call user's update method
                OnUpdate(deltaTime);

                if (Renderer is not null)
                    Renderer.DrawColor = System.Drawing.Color.PaleGoldenrod;

                // Progress the ECS world (execute systems)
                if (!World.IsDeferred()) // Basic check, might need more robust handling
                {
                    World.Progress(deltaTime);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error during App.Update: {ex}");
                Console.ResetColor();
                _shouldQuit = true; // Request quit on unhandled exception during update
                return false;
            }


            return !_shouldQuit; // Return true to continue, false to quit
        }

        /// <summary>
        /// Core event handling logic called by SdlHost.RunApplication.
        /// Translates SdlEventArgs into ECS events emitted on the AppEntity.
        /// Handles quit requests.
        /// </summary>
        /// <param name="evt">The SdlEventArgs object from the wrapper, or null.</param>
        /// <returns>True to continue, False to request exit.</returns>
        private bool HandleEvent(SdlEventArgs? evt)
        {
            if (evt == null) return !_shouldQuit; // Continue if event is null/unhandled

            try
            {
                // Handle global quit event first
                if (evt is QuitEventArgs)
                {
                    Console.WriteLine("Quit event received.");
                    _shouldQuit = true;
                    //AppEntity.Emit<QuitEvent>(); // Optionally emit an ECS event too
                    return false; // Signal quit
                }

                // Handle window-specific events
                if (evt is WindowEventArgs windowEvt)
                {
                    // Check if it's our main window (important if multiple windows exist)
                    if (Window != null && windowEvt.WindowId == Window.Id)
                    {
                        switch (windowEvt.EventType)
                        {
                            case WindowEventType.CloseRequested:
                                Console.WriteLine("Window close requested.");
                                _shouldQuit = true;
                                ////AppEntity.Emit(new WindowCloseEvent()); // Emit specific ECS event
                                return false; // Signal quit

                            case WindowEventType.Resized:
                                AppEntity.Emit(new WindowResize(Width: windowEvt.Data1, Height: windowEvt.Data2));
                                break;
                            case WindowEventType.Moved:
                                AppEntity.Emit(new WindowMovedEvent(X: windowEvt.Data1, Y: windowEvt.Data2));
                                break;
                            case WindowEventType.FocusGained:
                                //AppEntity.Emit(new WindowFocusGainedEvent());
                                break;
                            case WindowEventType.FocusLost:
                                //AppEntity.Emit(new WindowFocusLostEvent());
                                break;
                            case WindowEventType.Shown:
                                //AppEntity.Emit(new WindowShownEvent());
                                break;
                            case WindowEventType.Hidden:
                                //AppEntity.Emit(new WindowHiddenEvent());
                                break;
                            case WindowEventType.Minimized:
                                //AppEntity.Emit(new WindowMinimizedEvent());
                                break;
                            case WindowEventType.Maximized:
                                //AppEntity.Emit(new WindowMaximizedEvent());
                                break;
                            case WindowEventType.Restored:
                                //AppEntity.Emit(new WindowRestoredEvent());
                                break;
                            case WindowEventType.MouseEnter:
                                // Assuming SdlHost provides global mouse state access if needed
                                // SDL.SDL_GetGlobalMouseState(out float globalX, out float globalY);
                                AppEntity.Emit(new WindowMouseEnterEvent(0, false, 0, 0, 0)); // TODO: Populate with real data if needed/available
                                break;
                            case WindowEventType.MouseLeave:
                                // SDL.SDL_GetGlobalMouseState(out float globalX, out float globalY);
                                AppEntity.Emit(new WindowMouseLeaveEvent(0, false, 0, 0, 0)); // TODO: Populate with real data if needed/available
                                break;
                                // Add other WindowEventType cases as needed
                        }
                    }
                }
                // Handle Keyboard events
                else if (evt is KeyboardEventArgs keyEvt)
                {
                    // Example: Quit on Escape key press
                    if (keyEvt.IsDown && keyEvt.Key == SDLWrapper.Key.Escape) // Use wrapper's Key enum
                    {
                        Console.WriteLine("Escape key pressed.");
                        _shouldQuit = true;
                        // Optionally emit EscapeKeyEvent or similar
                        return false; // Signal quit
                    }

                    // Emit specific ECS events
                    if (keyEvt.IsDown)
                    {
                        AppEntity.Emit(new KeyDownEvent(
                            Keycode: keyEvt.Key, // Cast if necessary, depends on wrapper/ECS event types
                            Modifiers: keyEvt.Modifiers, // Cast if necessary
                            IsRepeat: keyEvt.IsRepeat
                        ));
                    }
                    else // IsUp
                    {
                        AppEntity.Emit(new KeyUpEvent(
                            Keycode: keyEvt.Key, // Cast if necessary
                            Modifiers: keyEvt.Modifiers // Cast if necessary
                        ));
                    }
                }
                // Handle Mouse Button events
                else if (evt is MouseButtonEventArgs buttonEvt)
                {
                    if (buttonEvt.IsDown)
                    {
                        AppEntity.Emit(new MouseButtonDownEvent(
                           MouseButton: (SDL.SDL_MouseButtonFlags)buttonEvt.Button, // Cast based on your event definition
                           X: buttonEvt.X,
                           Y: buttonEvt.Y,
                           Clicks: buttonEvt.Clicks
                        ));
                    }
                    else // IsUp
                    {
                        AppEntity.Emit(new MouseButtonUpEvent(
                             MouseButton: (SDL.SDL_MouseButtonFlags)buttonEvt.Button, // Cast based on your event definition
                            X: buttonEvt.X,
                            Y: buttonEvt.Y,
                            Clicks: buttonEvt.Clicks // Clicks might be 0 on up, verify wrapper behavior
                        ));
                    }
                }
                // Handle Mouse Motion events
                else if (evt is MouseMotionEventArgs motionEvt)
                {
                    AppEntity.Emit(new MouseMotionEvent(
                       MouseState: motionEvt.State, // Cast based on your event definition
                       X: motionEvt.X,
                       Y: motionEvt.Y,
                       XRel: motionEvt.XRel,
                       YRel: motionEvt.YRel
                    ));
                }
                // Handle Mouse Wheel events
                else if (evt is MouseWheelEventArgs wheelEvt)
                {
                    AppEntity.Emit(new MouseWheelEvent(
                       ScrollX: wheelEvt.ScrollX,
                       ScrollY: wheelEvt.ScrollY,
                       Direction: wheelEvt.Direction // Cast based on your event definition (e.g., SDL_MouseWheelDirection)
                    ));
                }
                // Handle text input events (if your wrapper provides them)
                else if (evt is TextInputEventArgs textEvt)
                {
                    //AppEntity.Emit(new TextInputEvent(textEvt.Text)); // Assuming simple text event
                }

                // Add handlers for other SdlEventArgs types as provided by your wrapper
                // (e.g., Controller Events, Joystick Events, Drop Events, etc.)

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error during App.HandleEvent: {ex}");
                Console.ResetColor();
                _shouldQuit = true; // Request quit on unhandled exception during event processing
                return false;
            }

            // Return true to continue processing, false if quit was requested
            return !_shouldQuit;
        }

        /// <summary>
        /// Core cleanup logic called by SdlHost.RunApplication just before exit.
        /// Calls the user-defined OnQuit method and disposes resources.
        /// </summary>
        private void Cleanup()
        {
            Console.WriteLine($"App.Cleanup: Thread {Environment.CurrentManagedThreadId}");
            Console.WriteLine("App.Cleanup started.");
            try
            {
                // Call user's cleanup code first
                OnQuit();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error during user OnQuit: {ex}");
                Console.ResetColor();
                // Continue with engine cleanup even if user code fails
            }

            Renderer?.Dispose();
            Window?.Dispose(); // Dispose wrapper objects

            Console.WriteLine("Window, Renderer, Engine, and World disposed.");
            Console.WriteLine("App.Cleanup finished.");
            // SdlHost.RunApplication handles SDL.Quit() automatically.
        }


        // --- Abstract / Virtual methods for derived classes ---

        /// <summary>
        /// Called once during application initialization after core components (Window, Renderer, ECS) are set up.
        /// Implement application-specific setup here (e.g., load resources, create initial entities/systems).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>True if initialization was successful, False otherwise (will cause immediate shutdown).</returns>
        protected abstract bool OnInit(string[] args);

        /// <summary>
        /// Called repeatedly for each frame/iteration of the application loop.
        /// Implement update logic (e.g., game state, physics) here. Rendering is typically handled
        /// by ECS systems invoked via World.Progress in the main Update loop, but direct rendering
        /// can also be done here if needed.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last call to OnUpdate, in seconds.</param>
        protected virtual void OnUpdate(float deltaTime)
        {
            // Base implementation does nothing. Override in derived class.
            // Example: base.OnUpdate(deltaTime); // Call if base class adds logic later
        }

        /// <summary>
        /// Called once just before the application terminates and before core resources (Window, Renderer) are disposed.
        /// Implement cleanup for resources created in OnInit here.
        /// </summary>
        protected abstract void OnQuit();

    }
}