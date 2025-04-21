
using System.Drawing;
using System.Runtime.InteropServices;
using AyanamisTower.NihilEx.ECS;
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

        // Instance variable to track time for delta time calculation
        private ulong _lastUpdateTimeTicks;
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

                // Initialize timing here, before OnInit is called.
                _lastUpdateTimeTicks = SDL.GetTicks();

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
        protected virtual SDL.AppResult OnInit(string[] args)
        {
            Engine = new(World);

            try
            {
                Window = new(
                    title: "Title",
                    width: 600,
                    height: 500,
                    isResizable: true);

                Renderer = Window?.CreateRenderer();

                World.Set(Window);
                World.Set(Renderer);
                return SDL.AppResult.Continue;

            }
            catch (Exception ex)
            {
                SDL.LogError(SDL.LogCategory.Error, ex.Message);
                return SDL.AppResult.Failure;
            }
        }

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
            return SDL.AppResult.Continue;
        }

        /// <summary>
        /// Called once just before the application terminates.
        /// Perform cleanup of resources created in OnInit here.
        /// Note: SDL.Quit() is called automatically by SDL after this.
        /// </summary>
        /// <param name="result">The result code that caused the application to quit.</param>
        protected virtual void OnQuit(SDL.AppResult result)
        {
            SDL.LogInfo(SDL.LogCategory.Application, $"Base OnQuit called with result: {result}");
            // Base implementation can Quit initialized subsystems
            if (SDL.WasInit(SDL.InitFlags.Video) != 0) // Check if Video was initialized
            {
                SDL.QuitSubSystem(SDL.InitFlags.Video); // Quit only what we initialized
            }
            // SDL.Quit(); // DO NOT CALL SDL.Quit() here, SDL does it after this callback.
        }


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
                return instance.OnInit(argv);
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
                ulong currentTimeTicks = SDL.GetTicks();
                float deltaTime = (currentTimeTicks - instance._lastUpdateTimeTicks) / 1000.0f;
                instance._lastUpdateTimeTicks = currentTimeTicks; // Update for next frame
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
                instance.OnQuit(result);

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
