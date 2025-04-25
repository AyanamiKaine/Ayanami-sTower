using AyanamisTower.NihilEx.ECS;
using AyanamisTower.NihilEx.ECS.Events;
using AyanamisTower.NihilEx.SDLWrapper; // Assuming SdlHost, SdlEventArgs etc. are here
using Flecs.NET.Core;
using SDL3;
using static SDL3.SDL; // Keep for SDL enums like Keycode if needed by user code or ECS events

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
#pragma warning disable CS0649 // Make field read-only
        private GpuDevice? _gpuDevice;

        private GpuCommandBuffer? _cmd;

        // Resources (store these)

        private GpuShader? _vertexShader;

        private GpuShader? _fragmentShader;
        private GpuGraphicsPipeline? _pipeline;
        private GpuBuffer? _vertexBuffer;
#pragma warning restore CS0649 // Make field read-only
        /// <summary>
        /// Gets or sets the current height of the application window.
        /// </summary>
        public int Height
        {
            get => Window?.Size.Y ?? 0;
            set
            {
                if (Window != null)
                    Window.Size = new(x: Width, y: value);
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
                if (Window != null)
                    Window.Size = new(x: value, y: Height);
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
                if (Window != null)
                    Window.Title = value;
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
            Console.WriteLine(value: "Starting SDL Application via SdlHost.RunApplication...");

            // SdlHost.RunApplication handles SDL_Init, SDL_Quit, and the event/update loop.
            int exitCode = SdlHost.RunApplication(
                init: Initialize, // Pass instance method directly
                update: Update, // Pass instance method directly
                eventHandler: HandleEvent, // Pass instance method directly
                quit: Cleanup, // Pass instance method directly
                args: args // Pass command line args
            );

            Console.WriteLine(value: $"Application finished with exit code: {exitCode}");
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
            Console.WriteLine(
                value: $"App.Initialize: Thread {Environment.CurrentManagedThreadId}"
            );
            try
            {
                // SdlHost.RunApplication handles SDL.Init based on its internal needs.

                // Initialize DeltaTime manager
                _deltaTimeManager.Initialize();

                // Create Engine and configure World
                Engine = new(world: World);
                //World.SetThreads(SdlHost.GetNumLogicalCores()); // Use SdlHost or SDL directly if available

                // Set singletons in the ECS world
                World.Set(data: _deltaTimeManager); // Make DeltaTime accessible via ECS

                // Create Window and Renderer using the wrapper's classes
                Window = new Window(
                    title: InitalTitle,
                    width: InitalWidth,
                    height: InitalHeight,
                    flags: WindowFlags.Resizable
                ); // Assuming WindowFlags enum exists
                Console.WriteLine(value: $"Window created with ID: {Window.Id}");
                if (Window == null)
                    throw new InvalidOperationException(message: "Failed to create SDL Window.");

                World.Set(data: Window); // Add Window as singleton resource

                Renderer = Window.CreateRenderer();
                Console.WriteLine(value: $"Renderer created: {Renderer.Name}");
                if (Renderer == null)
                    throw new InvalidOperationException(message: "Failed to create SDL Renderer.");
                Renderer.DrawColor = new Color(r: 255, g: 255, b: 255, a: 255); // White (using wrapper's Color)
                World.Set(data: Renderer); // Add Renderer as singleton resource

                // Create the application's root entity for events
                _appEntity = World.Entity(name: $"App: {Title}"); // Use property, will fetch from Window

                /*
                    _gpuDevice = new GpuDevice(GpuShaderFormat.SpirV, enableDebugMode: true);
                    Console.WriteLine($"Created GPU device with driver: {_gpuDevice.DriverName}");
                    _gpuDevice.ClaimWindow(Window);
                */
                // Call user's initialization code
                if (!OnInit(args: args))
                {
                    Console.Error.WriteLine(
                        value: "Application OnInit returned false. Shutting down."
                    );
                    _shouldQuit = true; // Signal immediate quit if OnInit fails
                    return false;
                }

                Console.WriteLine(value: "App.Initialize successful.");
                return !_shouldQuit; // Return true to continue
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(value: $"Error during App.Initialize: {ex}");
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
            if (_shouldQuit)
                return false; // Exit early if quit was requested

            try
            {
                // Calculate Delta Time
                _deltaTimeManager.Update();
                float deltaTime = _deltaTimeManager.DeltaSeconds;
                Keyboard.UpdateState();

                if (Renderer is not null)
                    Renderer.DrawColor = System.Drawing.Color.PaleGoldenrod;

                RenderFrame();

                // Progress the ECS world (execute systems)
                if (!World.IsDeferred()) // Basic check, might need more robust handling
                {
                    World.Progress(deltaTime: deltaTime);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(value: $"Error during App.Update: {ex}");
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
            if (evt == null)
                return !_shouldQuit; // Continue if event is null/unhandled

            try
            {
                // Handle global quit event first
                if (evt is QuitEventArgs)
                {
                    Console.WriteLine(value: "Quit event received.");
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
                                Console.WriteLine(value: "Window close requested.");
                                _shouldQuit = true;
                                ////AppEntity.Emit(new WindowCloseEvent()); // Emit specific ECS event
                                return false; // Signal quit

                            case WindowEventType.Resized:
                                AppEntity.Emit(
                                    payload: new WindowResize(
                                        Width: windowEvt.Data1,
                                        Height: windowEvt.Data2
                                    )
                                );
                                break;
                            case WindowEventType.Moved:
                                AppEntity.Emit(
                                    payload: new WindowMovedEvent(
                                        X: windowEvt.Data1,
                                        Y: windowEvt.Data2
                                    )
                                );
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
                                SDL.SDL_GetGlobalMouseState(out float globalX1, out float globalY1);
                                AppEntity.Emit(
                                    payload: new WindowMouseEnterEvent(
                                        MouseButton: (MouseButton)
                                            Mouse.GetPosition(out float localX1, out float localY1),
                                        LocalX: localX1,
                                        LocalY: localY1,
                                        GlobalX: globalX1,
                                        GlobalY: globalY1
                                    )
                                ); // TODO: Populate with real data if needed/available
                                break;
                            case WindowEventType.MouseLeave:
                                SDL.SDL_GetGlobalMouseState(out float globalX2, out float globalY2);
                                AppEntity.Emit(
                                    payload: new WindowMouseLeaveEvent(
                                        MouseButton: (MouseButton)
                                            Mouse.GetPosition(out float localX2, out float localY2),
                                        LocalX: localX2,
                                        LocalY: localY2,
                                        GlobalX: globalX2,
                                        GlobalY: globalY2
                                    )
                                ); // TODO: Populate with real data if needed/available
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
                        Console.WriteLine(value: "Escape key pressed.");
                        _shouldQuit = true;
                        // Optionally emit EscapeKeyEvent or similar
                        return false; // Signal quit
                    }

                    // Emit specific ECS events
                    if (keyEvt.IsDown)
                    {
                        AppEntity.Emit(
                            payload: new KeyDownEvent(
                                Keycode: keyEvt.Key,
                                Modifiers: keyEvt.Modifiers,
                                IsRepeat: keyEvt.IsRepeat
                            )
                        );
                    }
                    else // IsUp
                    {
                        AppEntity.Emit(
                            payload: new KeyUpEvent(
                                Keycode: keyEvt.Key,
                                Modifiers: keyEvt.Modifiers
                            )
                        );
                    }
                }
                // Handle Mouse Button events
                else if (evt is MouseButtonEventArgs buttonEvt)
                {
                    if (buttonEvt.IsDown)
                    {
                        AppEntity.Emit(
                            payload: new MouseButtonDownEvent(
                                MouseButton: buttonEvt.Button,
                                X: buttonEvt.X,
                                Y: buttonEvt.Y,
                                Clicks: buttonEvt.Clicks
                            )
                        );
                    }
                    else // IsUp
                    {
                        AppEntity.Emit(
                            payload: new MouseButtonUpEvent(
                                MouseButton: buttonEvt.Button,
                                X: buttonEvt.X,
                                Y: buttonEvt.Y,
                                Clicks: buttonEvt.Clicks // Clicks might be 0 on up, verify wrapper behavior
                            )
                        );
                    }
                }
                // Handle Mouse Motion events
                else if (evt is MouseMotionEventArgs motionEvt)
                {
                    AppEntity.Emit(
                        payload: new MouseMotionEvent(
                            MouseState: motionEvt.State,
                            X: motionEvt.X,
                            Y: motionEvt.Y,
                            XRel: motionEvt.XRel,
                            YRel: motionEvt.YRel
                        )
                    );
                }
                // Handle Mouse Wheel events
                else if (evt is MouseWheelEventArgs wheelEvt)
                {
                    AppEntity.Emit(
                        payload: new MouseWheelEvent(
                            ScrollX: wheelEvt.ScrollX,
                            ScrollY: wheelEvt.ScrollY,
                            Direction: wheelEvt.Direction // Cast based on your event definition (e.g., SDL_MouseWheelDirection)
                        )
                    );
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
                Console.Error.WriteLine(value: $"Error during App.HandleEvent: {ex}");
                Console.ResetColor();
                _shouldQuit = true; // Request quit on unhandled exception during event processing
                return false;
            }

            // Return true to continue processing, false if quit was requested
            return !_shouldQuit;
        }

        /// <summary>
        /// Rendering a frame on the GPU
        /// </summary>
        public void RenderFrame()
        {
            if (_gpuDevice == null || Window == null)
                return;

            _cmd = _gpuDevice.AcquireCommandBuffer();

            // Acquire swapchain texture
            if (
                !_cmd.WaitAndAcquireSwapchainTexture(
                    Window,
                    out IntPtr swapchainTextureHandle,
                    out uint w,
                    out uint h
                )
            )
            {
                Console.Error.WriteLine("Failed to acquire swapchain texture.");
                _cmd.Cancel(); // Important: Cancel if acquisition fails
                return;
            }

            // Define render target info using the acquired swapchain texture
            var colorTargetInfo = new SDL_GPUColorTargetInfo
            {
                texture = swapchainTextureHandle, // Use the acquired handle
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                clear_color = new FColor(
                    0.1f,
                    0.2f,
                    0.3f,
                    1.0f
                ) // Use FColor wrapper
                ,
                // Cycle is usually false for swapchain image itself in simple cases
            };
            Span<SDL_GPUColorTargetInfo> colorTargets = [colorTargetInfo];

            // --- Begin Render Pass ---
            using (var renderPass = _cmd.BeginRenderPass(colorTargets))
            {
                // ... more draw calls ...
            } // --- End Render Pass (Dispose called automatically) ---
            // --- Submit ---
            _cmd.Submit(); // Submit commands for this frame
            // Or:
            // GpuFence fence = _cmd.SubmitAndAcquireFence();
            // ... do CPU work ...
            // _gpuDevice.WaitForFence(fence); // Wait if needed
            // fence.Dispose();

            // SDL_RenderPresent is NOT used with the GPU API. Submission handles presentation implicitly.
        }

        /// <summary>
        /// Core cleanup logic called by SdlHost.RunApplication just before exit.
        /// Calls the user-defined OnQuit method and disposes resources.
        /// </summary>
        private void Cleanup()
        {
            Console.WriteLine(value: $"App.Cleanup: Thread {Environment.CurrentManagedThreadId}");
            Console.WriteLine(value: "App.Cleanup started.");
            try
            {
                // Call user's cleanup code first
                OnQuit();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(value: $"Error during user OnQuit: {ex}");
                Console.ResetColor();
                // Continue with engine cleanup even if user code fails
            }
            _pipeline?.Dispose();
            _vertexBuffer?.Dispose();
            // Dispose other resources...
            _vertexShader?.Dispose();
            _fragmentShader?.Dispose();

            _gpuDevice?.ReleaseWindow(Window!);
            _gpuDevice?.Dispose();
            Renderer?.Dispose();
            Window?.Dispose(); // Dispose wrapper objects

            Console.WriteLine(value: "Window, Renderer, Engine, and World disposed.");
            Console.WriteLine(value: "App.Cleanup finished.");
            // SdlHost.RunApplication handles SDL.Quit() automatically.
        }

        // --- Abstract  methods for derived classes ---

        /// <summary>
        /// Called once during application initialization after core components (Window, Renderer, ECS) are set up.
        /// Implement application-specific setup here (e.g., load resources, create initial entities/systems).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>True if initialization was successful, False otherwise (will cause immediate shutdown).</returns>
        protected abstract bool OnInit(string[] args);

        /// <summary>
        /// Called once just before the application terminates and before core resources (Window, Renderer) are disposed.
        /// Implement cleanup for resources created in OnInit here.
        /// </summary>
        protected abstract void OnQuit();
    }
}
