
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
        // Static handle to the current running App instance.
        // Limitation: This simple implementation assumes only one App instance runs via Run() at a time.
        private static GCHandle _appHandle;

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
            int exitCode = 0;
            try
            {
                // Allocate the GCHandle for this instance *before* entering callbacks.
                _appHandle = GCHandle.Alloc(this);
                IntPtr appStatePtr = GCHandle.ToIntPtr(_appHandle);


                // Create delegates pointing to our static callback wrappers.
                // Keep them alive for the duration of EnterAppMainCallbacks.
                SDL.AppInitFunc initFunc = StaticAppInit;
                SDL.AppIterateFunc iterateFunc = StaticAppIterate;
                SDL.AppEventFunc eventFunc = StaticAppEvent;
                SDL.AppQuitFunc quitFunc = StaticAppQuit;

                SDL.LogInfo(SDL.LogCategory.Application, "Entering SDL Main Callbacks...");
                exitCode = SDL.EnterAppMainCallbacks(args.Length, args, initFunc, iterateFunc, eventFunc, quitFunc);
                SDL.LogInfo(SDL.LogCategory.Application, $"Exited SDL Main Callbacks with code: {exitCode}");

            }
            catch (Exception ex)
            {
                SDL.LogError(SDL.LogCategory.Application, $"Unhandled exception during App Run: {ex}");
                exitCode = 1; // Indicate an error
            }
            finally
            {
                // CRITICAL: Free the GCHandle after EnterAppMainCallbacks returns.
                if (_appHandle.IsAllocated)
                {
                    _appHandle.Free();
                    SDL.LogInfo(SDL.LogCategory.Application, "App GCHandle Freed.");
                }
            }
            return exitCode;
        }

        // --- Virtual methods for derived classes to override ---

        /// <summary>
        /// Called once during application initialization.
        /// Perform SDL initialization, create windows/renderers, load resources here.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>SDL.AppResult.Continue to proceed, or Success/Failure to exit early.</returns>
        protected SDL.AppResult SDLInit(string[] args)
        {
            Engine = new(World);

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
                SDL.LogError(SDL.LogCategory.Error, ex.Message);
                return SDL.AppResult.Failure;
            }
        }

        /// <summary>
        /// Called after core SDL initialization (Window, Renderer, DeltaTime) is complete.
        /// Implement application-specific setup here, such as loading resources,
        /// configuring the Window/Renderer, and setting up ECS entities/systems.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>SDL.AppResult.Continue to proceed, or Success/Failure to exit early.</returns>
        protected abstract SDL.AppResult OnInit(string[] args);

        /// <summary>
        /// Called repeatedly for each frame/iteration of the application loop.
        /// Implement update logic and rendering here.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last call to OnIterate, in seconds.</param>
        /// <returns>SDL.AppResult.Continue to keep running, or Success/Failure to exit.</returns>
        protected virtual SDL.AppResult OnIterate(float deltaTime)
        {
            Renderer!.DrawColor = (RgbaColor)Color.White;
            World.Progress(deltaTime);
            return SDL.AppResult.Continue;
        }

        /// <summary>
        /// Called whenever an SDL event occurs.
        /// Handle input, window events, etc., here.
        /// </summary>
        /// <param name="e">The SDL_Event structure.</param>
        /// <returns>SDL.AppResult.Continue to keep running, or Success/Failure to exit.</returns>
        protected virtual SDL.AppResult OnEvent(ref SDL.Event e)
        {
            // Base implementation handles Quit event.
            if (e.Type == (uint)SDL.EventType.Quit)
            {
                SDL.LogInfo(SDL.LogCategory.Application, "Base OnEvent: Quit event received.");
                return SDL.AppResult.Success; // Signal graceful termination
            }
            else if (e.Type == (uint)SDL.EventType.WindowResized)
            {
                AppEntity.Emit(new WindowResize(e.Display.Data1, e.Display.Data2));
            }
            else if (e.Type == (uint)SDL.EventType.KeyDown)
            {
                AppEntity.Emit(new KeyDownEvent(
                    Keycode: e.Key.Key,
                    Modifiers: e.Key.Mod,
                    IsRepeat: e.Key.Repeat
                ));
            }
            else if (e.Type == (uint)SDL.EventType.KeyUp)
            {
                AppEntity.Emit(new KeyUpEvent(
                    Keycode: e.Key.Key,
                    Modifiers: e.Key.Mod
                ));
            }
            else if (e.Type == (uint)SDL.EventType.MouseButtonDown)
            {
                AppEntity.Emit(new MouseButtonDownEvent(
                    MouseButton: SDL.GetMouseState(out float _, out float _),
                    X: e.Button.X,
                    Y: e.Button.Y,
                    Clicks: e.Button.Clicks
                ));
            }
            else if (e.Type == (uint)SDL.EventType.MouseButtonUp)
            {
                AppEntity.Emit(new MouseButtonUpEvent(
                    MouseButton: SDL.GetMouseState(out float _, out float _),
                    X: e.Button.X,
                    Y: e.Button.Y,
                    Clicks: e.Button.Clicks
                ));
            }
            else if (e.Type == (uint)SDL.EventType.MouseMotion)
            {
                AppEntity.Emit(new MouseMotionEvent(
                    MouseState: SDL.GetMouseState(out float _, out float _),
                    X: e.Motion.X,
                    Y: e.Motion.Y,
                    XRel: e.Motion.XRel,
                    YRel: e.Motion.YRel));
            }
            else if (e.Type == (uint)SDL.EventType.MouseWheel)
            {
                AppEntity.Emit(new MouseWheelEvent(
                    ScrollX: e.Wheel.X,
                    ScrollY: e.Wheel.Y,
                    Direction: e.Wheel.Direction
                ));
            }

            return SDL.AppResult.Continue;
        }

        /// <summary>
        /// Called once just before the application terminates.
        /// Perform cleanup of resources created in OnInit here.
        /// Note: SDL.Quit() is called automatically by SDL after this.
        /// </summary>
        /// <param name="result">The result code that caused the application to quit.</param>
        protected void SDLQuit(SDL.AppResult result)
        {

            try
            {
                SDL.LogInfo(SDL.LogCategory.Application, "Calling OnUserQuit...");
                OnQuit(result); // Allow derived class to clean up its resources
                SDL.LogInfo(SDL.LogCategory.Application, "OnUserQuit finished.");
            }
            catch (Exception ex)
            {
                // Log error in user cleanup but continue with base cleanup
                SDL.LogError(SDL.LogCategory.Application, $"Exception during OnUserQuit: {ex}");
            }
            if (Renderer != null)
            {
                Renderer.Dispose();
                Renderer = null; // Set to null after disposal
                SDL.LogInfo(SDL.LogCategory.Application, "Renderer Disposed.");
            }

            // Dispose Window
            if (Window != null)
            {
                Window.Dispose();
                Window = null; // Set to null after disposal
                SDL.LogInfo(SDL.LogCategory.Application, "Window Disposed.");
            }

            SDL.LogInfo(SDL.LogCategory.Application, $"Base OnQuit called with result: {result}");
            // Base implementation can Quit initialized subsystems
            if (SDL.WasInit(SDL.InitFlags.Video) != 0) // Check if Video was initialized
            {
                SDL.QuitSubSystem(SDL.InitFlags.Video); // Quit only what we initialized
            }
            // SDL.Quit(); // DO NOT CALL SDL.Quit() here, SDL does it after this callback.
        }

        /// <summary>
        /// Called just before the application terminates and before core resources (Window, Renderer) are disposed.
        /// Implement cleanup for resources created in OnUserInitialize here.
        /// </summary>
        /// <param name="result">The result code that caused the application to quit.</param>
        protected abstract void OnQuit(SDL.AppResult result);

        // --- Static Callback Wrappers (Called by SDL) ---
        private static SDL.AppResult StaticAppInit(IntPtr appstatePtrRef, int argc, string[] argv)
        {
            try
            {
                // Retrieve the App instance from the static handle
                if (!_appHandle.IsAllocated || _appHandle.Target is not App instance)
                {
                    SDL.LogError(SDL.LogCategory.Application, "StaticAppInit: Failed to get App instance from handle.");
                    return SDL.AppResult.Failure;
                }

                // IMPORTANT: Tell SDL which state pointer to use for subsequent callbacks.
                // We pass the IntPtr representation of the GCHandle we already created in Run().
                Marshal.WriteIntPtr(appstatePtrRef, GCHandle.ToIntPtr(_appHandle));

                // Call the virtual OnInit method on the specific App instance
                return instance.SDLInit(argv);
            }
            catch (Exception ex)
            {
                SDL.LogError(SDL.LogCategory.Application, $"Exception in StaticAppInit: {ex}");
                return SDL.AppResult.Failure;
            }
        }

        private static SDL.AppResult StaticAppIterate(IntPtr appstatePtr)
        {
            try
            {
                // Retrieve the App instance from the handle passed by SDL
                if (appstatePtr == IntPtr.Zero) return SDL.AppResult.Failure;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.LogError(SDL.LogCategory.Application, "StaticAppIterate: Failed to get App instance from handle.");
                    return SDL.AppResult.Failure;
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
                SDL.LogError(SDL.LogCategory.Application, $"Exception in StaticAppIterate: {ex}");
                return SDL.AppResult.Failure;
            }
        }

        private static SDL.AppResult StaticAppEvent(IntPtr appstatePtr, ref SDL.Event e)
        {
            try
            {
                // Retrieve the App instance
                if (appstatePtr == IntPtr.Zero) return SDL.AppResult.Failure;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.LogError(SDL.LogCategory.Application, "StaticAppEvent: Failed to get App instance from handle.");
                    return SDL.AppResult.Failure;
                }

                // Call the virtual OnEvent method
                return instance.OnEvent(ref e);
            }
            catch (Exception ex)
            {
                SDL.LogError(SDL.LogCategory.Application, $"Exception in StaticAppEvent: {ex}");
                return SDL.AppResult.Failure; // Or Continue depending on desired robustness
            }
        }

        private static void StaticAppQuit(IntPtr appstatePtr, SDL.AppResult result)
        {
            try
            {
                // Retrieve the App instance
                if (appstatePtr == IntPtr.Zero) return;
                GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
                if (!handle.IsAllocated || handle.Target is not App instance)
                {
                    SDL.LogError(SDL.LogCategory.Application, "StaticAppQuit: Failed to get App instance from handle.");
                    return;
                }

                // Call the virtual OnQuit method
                instance.SDLQuit(result);

                // NOTE: Do not free the GCHandle here. The Run() method does it in its finally block.
            }
            catch (Exception ex)
            {
                SDL.LogError(SDL.LogCategory.Application, $"Exception in StaticAppQuit: {ex}");
                // Don't re-throw, as we're already quitting.
            }
        }
    }
}
