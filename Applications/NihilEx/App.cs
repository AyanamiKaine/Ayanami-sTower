
using System.Drawing;
using System.Runtime.InteropServices;
using AyanamisTower.NihilEx.ECS;
using AyanamisTower.NihilEx.ECS.Events;
using Flecs.NET.Core;
using SDL3;


namespace AyanamisTower.NihilEx
{
    /// <summary>
    /// Base class for creating SDL applications using the callback mechanism.
    /// Inherit from this class and override the OnInitialize, OnUpdate, OnEvent, and OnQuit methods.
    /// </summary>
    public abstract class App
    {
        private Entity _appEntity;
        /// <summary>
        /// Gets the Flecs Entity associated with this application instance.
        /// </summary>
        /// <remarks>
        /// Use this entity to register observers for events. This provides a central point
        /// for hooking into application-level or engine-level events managed by Flecs.
        /// </remarks>
        /// <example>
        /// The following code demonstrates how to register an observer for the <c>WindowResize</c> event
        /// using the <c>AppEntity</c> property:
        /// <code><![CDATA[
        /// // Get the application entity (assuming 'myApp' is an instance of your class)
        /// var appEntity = myApp.AppEntity;
        ///
        /// // Register an observer for the WindowResize event.
        /// // This lambda expression will be executed whenever the WindowResize event occurs.
        /// appEntity.Observe<WindowResize>(() =>
        /// {
        ///     Console.WriteLine("Window resize event observed via AppEntity!");
        /// });
        /// ]]></code>
        /// </example>
        /// <value>The application's root <see cref="Entity"/>.</value>
        public Entity AppEntity { get => _appEntity; }

        // Instance of our new DeltaTime class
        private readonly DeltaTime _deltaTimeManager = new();
        /// <summary>
        /// Gets the SDL Renderer associated with this application instance.
        /// This is typically created during the OnInit phase alongside the Window.
        /// </summary>
        protected Renderer? Renderer { get; private set; }
        /// <summary>
        /// Gets the SDL Window associated with this application instance.
        /// This is typically created during the OnInit phase.
        /// </summary>
        protected Window? Window { get; private set; }
        /// <summary>
        /// Height of the app
        /// </summary>
        public int Height
        {
            get
            {
                return Window?.Height ?? 0;
            }
            set
            {
                if (Window is null)
                    return;

                Window.Height = value;
            }
        }
        /// <summary>
        /// Width of the app
        /// </summary>
        public int Width
        {
            get
            {
                return Window?.Width ?? 0;
            }
            set
            {
                if (Window is null)
                    return;

                Window.Width = value;
            }
        }
        /// <summary>
        /// Title of the app
        /// </summary>
        public string Title
        {
            get
            {
                return Window?.Title ?? "";
            }
            set
            {
                if (Window is null)
                    return;

                Window.Title = value;
            }
        }

        /// <summary>
        /// Start height of the app
        /// </summary>
        public required int InitalHeight { init; get; }
        /// <summary>
        /// Start width of the app
        /// </summary>
        public required int InitalWidth { init; get; }
        /// <summary>
        /// Start title of the app
        /// </summary>
        public required string InitalTitle { init; get; }
        /// <summary>
        /// ECS Flecs World
        /// </summary>
        public World World { get; } = World.Create();
        /// <summary>
        /// Gets the Engine instance associated with this application.
        /// </summary>
        public Engine? Engine { get; private set; }
        /// <summary>
        /// Runs the SDL application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        /// <returns>The exit code of the application.</returns>
        public int Run(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "Detected Windows. Using EnterAppMainCallbacks.");
                return RunWindowsCallbacks(args);
                throw new Exception("NOT IMPLEMENTED");
            }
            else
            {
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "Detected Non-Windows OS. Using manual event loop.");
                return RunManualLoop(args);
            }
        }

        private unsafe int RunWindowsCallbacks(string[] args)
        {
            int exitCode = 0;
            GCHandle appHandle = default; // GCHandle is local to this method now

            try
            {
                appHandle = GCHandle.Alloc(this);
                IntPtr handlePtr = GCHandle.ToIntPtr(appHandle);
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                    $"RunWindowsCallbacks: Thread {Environment.CurrentManagedThreadId}: Allocating handle {handlePtr:X}");

                // --- Critical Section (Implicit) ---
                // Set the shared static handle. ASSUMES no other thread is doing this concurrently.
                _pendingAppHandle = appHandle;
                // --- End Critical Section ---

                bool isAllocatedAfterSet = _pendingAppHandle.IsAllocated;
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                    $"RunWindowsCallbacks: Thread {Environment.CurrentManagedThreadId}: Set _pendingAppHandle. IsAllocated = {isAllocatedAfterSet}");


                // Create delegates pointing to our static callback wrappers.
                SDL.SDL_AppInit_func initFunc = StaticAppInit;
                SDL.SDL_AppIterate_func iterateFunc = StaticAppIterate;
                SDL.SDL_AppEvent_func eventFunc = StaticAppEvent;
                SDL.SDL_AppQuit_func quitFunc = StaticAppQuit;

                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "Entering SDL Main Callbacks...");
                // Pass the GCHandle's IntPtr via the initial state parameter mechanism
                // (Note: SDL_EnterAppMainCallbacks expects the address of where to store the state pointer)
                // We'll handle setting the state pointer inside StaticAppInit
                exitCode = SDL.SDL_EnterAppMainCallbacks(args.Length, IntPtr.Zero, initFunc, iterateFunc, eventFunc, quitFunc);
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exited SDL Main Callbacks with code: {exitCode}");

                // Check for errors *after* returning, in case it exited immediately with an error
                string sdlError = SDL.SDL_GetError();
                if (!string.IsNullOrEmpty(sdlError))
                {
                    SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"SDL Error reported after Exit: {sdlError}");
                    if (exitCode == 0 && !sdlError.Contains("No error")) // Avoid overriding valid non-zero exit codes
                    {
                        exitCode = 1; // Indicate error if exited cleanly but SDL has an error string
                    }
                }
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Unhandled exception during Windows RunCallbacks: {ex}");
                exitCode = 1; // Indicate an error
            }
            finally
            {
                // CRITICAL: Free the GCHandle after EnterAppMainCallbacks returns.
                if (appHandle.IsAllocated)
                {
                    appHandle.Free();
                    //SDL.LogInfo(SDL.LogCategory.Application, "App GCHandle Freed.");
                }
                // NOTE: SDL automatically calls SDL_Quit() after the callbacks finish/return.
            }
            return exitCode;
        }


        // --- Manual Event Loop for Non-Windows Platforms ---
        private int RunManualLoop(string[] args)
        {
            SDL.SDL_AppResult appResult = SDL.SDL_AppResult.SDL_APP_CONTINUE; // Track why the loop exits

            try
            {
                // 1. Explicit Initialization
                // Use flags appropriate for your application (Video is common)
                if (!SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO | SDL.SDL_InitFlags.SDL_INIT_EVENTS /* Add other flags as needed */))
                {
                    SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Manual Loop: SDL.Init failed: {SDL.SDL_GetError()}");
                    return 1; // Early exit on failure
                }
                //SDL.SDL_LogInfo(SDL.LogCategory.Application, "Manual Loop: SDL.Init successful.");

                // Call the same core initialization logic (creates Window, Renderer, calls user OnInit)
                appResult = SDLInit(args);
                if (appResult != SDL.SDL_AppResult.SDL_APP_CONTINUE)
                {
                    //SDL.LogInfo(SDL.LogCategory.Application, $"Manual Loop: SDLInit returned {appResult}. Exiting early.");
                    // Need to call SDLQuit before SDL.Quit if init partially succeeded
                    SDLQuit(appResult);
                    SDL.SDL_Quit();
                    return (appResult == SDL.SDL_AppResult.SDL_APP_SUCCESS) ? 0 : 1;
                }
                //SDL.LogInfo(SDL.LogCategory.Application, "Manual Loop: SDLInit successful.");

                // Initialize DeltaTime manager (already done in SDLInit, but ensure it's ready)
                _deltaTimeManager.Initialize(); // Safe to call again if already initialized

                // 2. Main Loop
                bool loop = true;
                while (loop)
                {
                    // --- Calculate Delta Time ---
                    _deltaTimeManager.Update();
                    float deltaTime = _deltaTimeManager.DeltaSeconds;
                    // --- End Delta Time ---

                    // --- Event Handling ---
                    while (SDL.SDL_PollEvent(out SDL.SDL_Event e))
                    {
                        // Call the user's event handler
                        appResult = OnEvent(ref e);
                        if (appResult != SDL.SDL_AppResult.SDL_APP_CONTINUE)
                        {
                            //SDL.LogInfo(SDL.LogCategory.Application, $"Manual Loop: OnEvent returned {appResult}. Exiting loop.");
                            loop = false;
                            break; // Exit inner event loop
                        }
                    }

                    if (!loop) break; // Exit outer loop if event handling requested it

                    // --- Update and Render ---
                    // Call the user's update/render method
                    appResult = OnIterate(deltaTime);
                    if (appResult != SDL.SDL_AppResult.SDL_APP_CONTINUE)
                    {
                        //SDL.LogInfo(SDL.LogCategory.Application, $"Manual Loop: OnIterate returned {appResult}. Exiting loop.");
                        loop = false;
                    }

                    // Note: OnIterate is assumed to handle rendering, including SDL.RenderPresent()
                    // If not, add SDL.RenderPresent(Renderer); here.
                }
            }
            catch (Exception)
            {
                //SDL.LogError(SDL.LogCategory.Application, $"Unhandled exception during Manual Loop: {ex}");
                appResult = SDL.SDL_AppResult.SDL_APP_FAILURE; // Ensure cleanup happens correctly
            }
            finally
            {
                // 3. Cleanup
                //SDL.LogInfo(SDL.LogCategory.Application, $"Manual Loop: Exited with result {appResult}. Cleaning up...");
                // Call the user's cleanup logic (disposes Window/Renderer, calls user OnQuit)
                SDLQuit(appResult);

                // Explicitly Quit SDL for the manual loop
                SDL.SDL_Quit();
                //SDL.LogInfo(SDL.LogCategory.Application, "Manual Loop: SDL.Quit() called.");
            }

            // Return 0 for success, 1 for failure
            return (appResult == SDL.SDL_AppResult.SDL_APP_SUCCESS) ? 0 : 1;
        }

        // --- Virtual methods for derived classes to override ---

        /// <summary>
        /// Called once during application initialization.
        /// Perform SDL initialization, create windows/renderers, load resources here.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>SDL.AppResult.Continue to proceed, or Success/Failure to exit early.</returns>
        protected SDL.SDL_AppResult SDLInit(string[] args)
        {
            Engine = new(World);
            World.SetThreads(SDL.SDL_GetNumLogicalCPUCores());
            try
            {
                // Initialize DeltaTime manager
                _deltaTimeManager.Initialize();
                // Register DeltaTime as a singleton component in the ECS world.
                // This allows any system to easily query it using world.Get<DeltaTime>().
                World.Set(_deltaTimeManager);

                Window = new(
                    title: InitalTitle,
                    width: InitalWidth,
                    height: InitalHeight,
                    isResizable: true);

                Renderer = Window?.CreateRenderer();
                Renderer!.DrawColor = (RgbaColor)Color.White;

                _appEntity = World.Entity($"App: {Title}");

                World.Set(Window);
                World.Set(Renderer);
                return OnInit(args);
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR, ex.Message);
                return SDL.SDL_AppResult.SDL_APP_FAILURE;
            }
        }

        /// <summary>
        /// Called after core SDL initialization (Window, Renderer, DeltaTime) is complete.
        /// Implement application-specific setup here, such as loading resources,
        /// configuring the Window/Renderer, and setting up ECS entities/systems.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>SDL.AppResult.Continue to proceed, or Success/Failure to exit early.</returns>
        protected abstract SDL.SDL_AppResult OnInit(string[] args);

        /// <summary>
        /// Called repeatedly for each frame/iteration of the application loop.
        /// Implement update logic and rendering here.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last call to OnIterate, in seconds.</param>
        /// <returns>SDL.AppResult.Continue to keep running, or Success/Failure to exit.</returns>
        protected virtual SDL.SDL_AppResult OnIterate(float deltaTime)
        {
            Renderer!.DrawColor = (RgbaColor)Color.White;
            World.Progress(deltaTime);
            return SDL.SDL_AppResult.SDL_APP_CONTINUE;
        }

        /// <summary>
        /// Called whenever an SDL event occurs.
        /// Handle input, window events, etc., here.
        /// </summary>
        /// <param name="e">The SDL_Event structure.</param>
        /// <returns>SDL.AppResult.Continue to keep running, or Success/Failure to exit.</returns>
        protected SDL.SDL_AppResult OnEvent(ref SDL.SDL_Event e)
        {
            if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_QUIT)
            {
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "Base OnEvent: Quit event received.");
                return SDL.SDL_AppResult.SDL_APP_SUCCESS; // Signal graceful termination
            }

            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_RESIZED)
            {
                AppEntity.Emit(new WindowResize(e.display.data1, e.display.data2));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_KEY_DOWN)
            {
                AppEntity.Emit(new KeyDownEvent(
                    Keycode: (SDL.SDL_Keycode)e.key.key,
                    Modifiers: e.key.mod,
                    IsRepeat: e.key.repeat
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_KEY_UP)
            {
                AppEntity.Emit(new KeyUpEvent(
                    Keycode: (SDL.SDL_Keycode)e.key.key,
                    Modifiers: e.key.mod
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN)
            {
                AppEntity.Emit(new MouseButtonDownEvent(
                    MouseButton: SDL.SDL_GetMouseState(out float _, out float _),
                    X: e.button.x,
                    Y: e.button.y,
                    Clicks: e.button.clicks
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP)
            {
                AppEntity.Emit(new MouseButtonUpEvent(
                    MouseButton: SDL.SDL_GetMouseState(out float _, out float _),
                    X: e.button.x,
                    Y: e.button.y,
                    Clicks: e.button.clicks
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_MOTION)
            {
                AppEntity.Emit(new MouseMotionEvent(
                    MouseState: SDL.SDL_GetMouseState(out float _, out float _),
                    X: e.motion.x,
                    Y: e.motion.y,
                    XRel: e.motion.xrel,
                    YRel: e.motion.yrel));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_WHEEL)
            {
                AppEntity.Emit(new MouseWheelEvent(
                    ScrollX: e.wheel.x,
                    ScrollY: e.wheel.y,
                    Direction: e.wheel.direction
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_MOVED)
            {
                uint windowId = e.window.windowID;
                SDL.SDL_GetWindowPosition(SDL.SDL_GetWindowFromID(windowId), out int newWidth, out int newHeight); // Safer way

                AppEntity.Emit(new WindowMovedEvent(
                    X: newWidth,
                    Y: newHeight
                ));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER)
            {
                AppEntity.Emit(new WindowMouseEnterEvent(
                    MouseButton: SDL.SDL_GetGlobalMouseState(out float X, out float Y),
                    Down: e.button.down,
                    X: X,
                    Y: Y,
                    Clicks: e.button.clicks));
            }
            else if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE)
            {
                AppEntity.Emit(new WindowMouseLeaveEvent(
                    MouseButton: SDL.SDL_GetGlobalMouseState(out float X, out float Y),
                    Down: e.button.down,
                    X: X,
                    Y: Y,
                    Clicks: e.button.clicks));
            }


            return SDL.SDL_AppResult.SDL_APP_CONTINUE;
        }

        /// <summary>
        /// Called once just before the application terminates.
        /// Perform cleanup of resources created in OnInit here.
        /// Note: SDL.Quit() is called automatically by SDL after this.
        /// </summary>
        /// <param name="result">The result code that caused the application to quit.</param>
        protected void SDLQuit(SDL.SDL_AppResult result)
        {

            try
            {
                //SDL.LogInfo(SDL.LogCategory.Application, "Calling OnUserQuit...");
                OnQuit(result); // Allow derived class to clean up its resources
                //SDL.LogInfo(SDL.LogCategory.Application, "OnUserQuit finished.");
            }
            catch (Exception ex)
            {
                // Log error in user cleanup but continue with base cleanup
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception during OnUserQuit: {ex}");
            }
            if (Renderer != null)
            {
                Renderer.Dispose();
                Renderer = null; // Set to null after disposal
                //SDL.LogInfo(SDL.LogCategory.Application, "Renderer Disposed.");
            }

            // Dispose Window
            if (Window != null)
            {
                Window.Dispose();
                Window = null; // Set to null after disposal
                //SDL.LogInfo(SDL.LogCategory.Application, "Window Disposed.");
            }

            //SDL.LogInfo(SDL.LogCategory.Application, $"Base OnQuit called with result: {result}");
            // Base implementation can Quit initialized subsystems
            if (SDL.SDL_WasInit(SDL.SDL_InitFlags.SDL_INIT_VIDEO) != 0) // Check if Video was initialized
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_InitFlags.SDL_INIT_VIDEO); // Quit only what we initialized
            }
            // SDL.Quit(); // DO NOT CALL SDL.Quit() here, SDL does it after this callback.
        }

        /// <summary>
        /// Called just before the application terminates and before core resources (Window, Renderer) are disposed.
        /// Implement cleanup for resources created in OnUserInitialize here.
        /// </summary>
        /// <param name="result">The result code that caused the application to quit.</param>
        protected abstract void OnQuit(SDL.SDL_AppResult result);

        // --- Static Callback Wrappers (Called by SDL) ---
        // Temporary static handle used ONLY during the transition into EnterAppMainCallbacks
        // This is a workaround pattern for C APIs without explicit user_data in init.
        private static GCHandle _pendingAppHandle;
        private static SDL.SDL_AppResult StaticAppInit(IntPtr appstatePtrRef, int argc, IntPtr argv)
        {
            // Retrieve the App instance via the GCHandle passed implicitly at first, then set state pointer
            GCHandle handle = default;
            App? instance = null;
            try
            {
                // On first call, appstatePtrRef might point to something temporary or null,
                // we need to retrieve our handle passed during GCHandle.Alloc
                // SDL's mechanism here can be tricky. Let's assume the *first* call needs us to
                // find our handle and *tell* SDL what pointer to use subsequently.
                // A common pattern is to pass the GCHandle.ToIntPtr() via the args or environment,
                // but SDL_EnterAppMainCallbacks doesn't directly support that.
                // The intended way is usually via the appstate pointer itself.

                // We rely on GCHandle.Alloc happening *before* this call in RunWindowsCallbacks
                // Find the *specific* handle we allocated just before the call.
                // This is tricky and potentially fragile if multiple App instances exist.
                // Reverting to the static handle idea might be necessary IF this proves unreliable,
                // OR rethinking how the handle is passed.
                // Let's *assume* for now that the GCHandle needs to be retrieved globally/statically
                // for this mechanism to work robustly without modifying SDL's expected pattern.

                // *** SAFER APPROACH: Use a temporary static variable during the callback setup ***
                // This is a common workaround for C APIs lacking explicit user data pointers in init.
                if (_pendingAppHandle.IsAllocated)
                {
                    handle = _pendingAppHandle;
                    instance = handle.Target as App;
                    if (instance == null)
                    {
                        SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppInit: Failed to get App instance from pending handle.");
                        return SDL.SDL_AppResult.SDL_APP_FAILURE;
                    }
                    // IMPORTANT: Tell SDL which state pointer to use for subsequent callbacks.
                    Marshal.WriteIntPtr(appstatePtrRef, GCHandle.ToIntPtr(handle));
                    SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppInit: Set SDL app state pointer from pending handle.");
                    _pendingAppHandle = default; // Clear the temporary static handle
                }
                else
                {
                    // This case should ideally not happen if called correctly by SDL via EnterAppMainCallbacks
                    SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppInit: No pending App handle found!");
                    return SDL.SDL_AppResult.SDL_APP_FAILURE;
                }


                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppInit: Calling instance.SDLInit...");
                SDL.SDL_AppResult initResult = instance.SDLInit([]);
                SDL.SDL_LogInfo((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"StaticAppInit: instance.SDLInit returned: {initResult}");
                return initResult;
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in StaticAppInit: {ex}");
                // Clean up static handle if exception occurs after acquisition but before clearing
                if (_pendingAppHandle.IsAllocated) _pendingAppHandle = default;
                return SDL.SDL_AppResult.SDL_APP_FAILURE;
            }
        }

        private static SDL.SDL_AppResult StaticAppIterate(IntPtr appstatePtr)
        {
            try
            {
                // Retrieve the App instance from the handle passed by SDL
                if (appstatePtr == IntPtr.Zero) return SDL.SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppIterate: Failed to get App instance from handle.");
                    return SDL.SDL_AppResult.SDL_APP_FAILURE;
                }

                // --- Calculate Delta Time ---
                instance._deltaTimeManager.Update();
                float deltaTime = instance._deltaTimeManager.DeltaSeconds;
                // --- End Delta Time ---

                // Call the virtual OnIterate method
                return instance.OnIterate(deltaTime);
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in StaticAppIterate: {ex}");
                return SDL.SDL_AppResult.SDL_APP_FAILURE;
            }
        }

        private static unsafe SDL.SDL_AppResult StaticAppEvent(nint appstatePtr, SDL.SDL_Event* e)
        {
            try
            {
                // Retrieve the App instance
                if (appstatePtr == IntPtr.Zero) return SDL.SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppEvent: Failed to get App instance from handle.");
                    return SDL.SDL_AppResult.SDL_APP_FAILURE;
                }

                SDL.SDL_Event eventValue = *e;

                // Call the virtual OnEvent method
                return instance.OnEvent(ref eventValue);
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in StaticAppEvent: {ex}");
                return SDL.SDL_AppResult.SDL_APP_FAILURE; // Or Continue depending on desired robustness
            }
        }

        private static void StaticAppQuit(IntPtr appstatePtr, SDL.SDL_AppResult result)
        {
            try
            {
                // Retrieve the App instance
                if (appstatePtr == IntPtr.Zero) return;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "StaticAppQuit: Failed to get App instance from handle.");
                    return;
                }

                // Call the virtual OnQuit method
                instance.SDLQuit(result);

                // NOTE: Do not free the GCHandle here. The Run() method does it in its finally block.
            }
            catch (Exception ex)
            {
                SDL.SDL_LogError((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in StaticAppQuit: {ex}");
                // Don't re-throw, as we're already quitting.
            }
        }

    }
}
