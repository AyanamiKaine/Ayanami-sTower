using System.Runtime.InteropServices;
using Flecs.NET.Core;
using SDL3;

namespace SDL3Example;


/*
In this example we are using the new callback way of using SDL3.
*/

internal static class Program
{
    private class AppState
    {
        public nint Window
        {
            get; set;
        }
        public nint Renderer
        {
            get; set;
        }
        public World World;

        // State for color cycling
        public float CurrentR { get; set; }
        public float CurrentG { get; set; }
        public float CurrentB { get; set; }

        public float DeltaR { get; set; } // Change per second
        public float DeltaG { get; set; } // Change per second
        public float DeltaB { get; set; } // Change per second

        public ulong LastUpdateTimeTicks { get; set; } // SDL Ticks (ms)
    }

    private static SDL.AppResult MyAppInit(IntPtr appstatePtrRef, int argc, string[] argv)
    {
        SDL.LogInfo(SDL.LogCategory.Application, "Application Initializing...");

        // Initialize SDL
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            return SDL.AppResult.Failure; // Signal failure
        }

        // Create Window and Renderer
        if (!SDL.CreateWindowAndRenderer("SDL3 Create Window (Callbacks)", 800, 600, SDL.WindowFlags.Resizable, out var window, out var renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
            SDL.Quit(); // Clean up initialized subsystems before failing
            return SDL.AppResult.Failure; // Signal failure
        }

        // --- Color Cycling Initialization ---
        const float startR = 100f;
        const float startG = 149f;
        const float startB = 237f;

        // Define how many units each color component changes per second
        const float changeRateR = 30.0f; // Red changes slower
        const float changeRateG = 50.0f; // Green changes medium
        const float changeRateB = 70.0f; // Blue changes faster

        // --- End Color Cycling Initialization ---
        // Create our state object
        var appState = new AppState
        {
            Window = window,
            Renderer = renderer,
            World = World.Create(),
            // Initialize color state
            CurrentR = startR,
            CurrentG = startG,
            CurrentB = startB,
            DeltaR = changeRateR,
            DeltaG = changeRateG,
            DeltaB = changeRateB,
            LastUpdateTimeTicks = SDL.GetTicks() // Get initial time
        };

        // Allocate a GCHandle to keep the managed state object alive
        // and get an IntPtr to pass back to SDL.
        try
        {
            GCHandle handle = GCHandle.Alloc(appState);
            // Write the handle's IntPtr representation into the location pointed to by appstatePtrRef
            Marshal.WriteIntPtr(appstatePtrRef, GCHandle.ToIntPtr(handle));

            SDL.LogInfo(SDL.LogCategory.Application, "Initialization Complete.");
            return SDL.AppResult.Continue; // Signal success and continue
        }
        catch (Exception ex)
        {
            SDL.LogError(SDL.LogCategory.Application, $"Failed to allocate GCHandle for app state: {ex.Message}");
            SDL.DestroyRenderer(renderer);
            SDL.DestroyWindow(window);
            SDL.Quit();
            return SDL.AppResult.Failure;
        }
    }

    private static SDL.AppResult MyAppIterate(IntPtr appstatePtr)
    {
        // Retrieve the GCHandle and AppState
        if (appstatePtr == IntPtr.Zero)
            return SDL.AppResult.Failure; // Should not happen if Init succeeded
        GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
        if (!handle.IsAllocated || handle.Target is not AppState appState)
        {
            SDL.LogError(SDL.LogCategory.Application, "Invalid AppState in Iterate.");
            return SDL.AppResult.Failure;
        }

        // --- Time Calculation ---
        ulong currentTimeTicks = SDL.GetTicks();
        // Calculate delta time in seconds (ticks are milliseconds)
        float deltaTime = (currentTimeTicks - appState.LastUpdateTimeTicks) / 1000.0f;
        appState.LastUpdateTimeTicks = currentTimeTicks; // Update for next frame
        // --- End Time Calculation ---

        // --- Color Update Logic ---
        // Update Red
        appState.CurrentR += appState.DeltaR * deltaTime;
        if (appState.CurrentR > 255.0f)
        {
            appState.CurrentR = 255.0f;
            appState.DeltaR *= -1.0f; // Reverse direction
        }
        else if (appState.CurrentR < 0.0f)
        {
            appState.CurrentR = 0.0f;
            appState.DeltaR *= -1.0f; // Reverse direction
        }

        // Update Green
        appState.CurrentG += appState.DeltaG * deltaTime;
        if (appState.CurrentG > 255.0f)
        {
            appState.CurrentG = 255.0f;
            appState.DeltaG *= -1.0f;
        }
        else if (appState.CurrentG < 0.0f)
        {
            appState.CurrentG = 0.0f;
            appState.DeltaG *= -1.0f;
        }

        // Update Blue
        appState.CurrentB += appState.DeltaB * deltaTime;
        if (appState.CurrentB > 255.0f)
        {
            appState.CurrentB = 255.0f;
            appState.DeltaB *= -1.0f;
        }
        else if (appState.CurrentB < 0.0f)
        {
            appState.CurrentB = 0.0f;
            appState.DeltaB *= -1.0f;
        }
        // --- End Color Update Logic ---

        appState.World.Progress(deltaTime);

        // --- Rendering ---
        // Set the *updated* draw color before clearing
        SDL.SetRenderDrawColor(appState.Renderer,
                               (byte)appState.CurrentR,
                               (byte)appState.CurrentG,
                               (byte)appState.CurrentB,
                               255); // Opaque Alpha

        // Rendering logic (formerly in the while loop)
        SDL.RenderClear(appState.Renderer);
        // Add other rendering here if needed
        SDL.RenderPresent(appState.Renderer);

        return SDL.AppResult.Continue; // Keep iterating
    }

    private static SDL.AppResult MyAppEvent(IntPtr appstatePtr, ref SDL.Event e)
    {
        // Retrieve the GCHandle and AppState (optional if not needed for event handling)
        // GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
        // AppState appState = handle.Target as AppState;

        // Event handling logic (formerly in the PollEvent loop)
        if (e.Type == (uint)SDL.EventType.Quit)
        {
            SDL.LogInfo(SDL.LogCategory.Application, "Quit event received.");
            return SDL.AppResult.Success; // Signal graceful termination
        }

        // Handle other events here if needed

        return SDL.AppResult.Continue; // Continue processing events/iterating
    }

    private static void MyAppQuit(IntPtr appstatePtr, SDL.AppResult result)
    {
        SDL.LogInfo(SDL.LogCategory.Application, $"Application Quitting with result: {result}");

        // Retrieve the GCHandle and AppState for cleanup
        if (appstatePtr != IntPtr.Zero)
        {
            GCHandle handle = GCHandle.FromIntPtr(appstatePtr);
            if (handle.IsAllocated)
            {
                if (handle.Target is AppState appState)
                {
                    // Cleanup resources held by AppState
                    SDL.LogInfo(SDL.LogCategory.Application, "Destroying Renderer and Window...");
                    SDL.DestroyRenderer(appState.Renderer);
                    SDL.DestroyWindow(appState.Window);
                }
                else
                {
                    SDL.LogWarn(SDL.LogCategory.Application, "App state handle target was not the expected type during Quit.");
                }

                // *** CRITICAL: Free the GCHandle ***
                handle.Free();
                SDL.LogInfo(SDL.LogCategory.Application, "GCHandle Freed.");
            }
            else
            {
                SDL.LogWarn(SDL.LogCategory.Application, "App state handle was already freed or invalid during Quit.");
            }
        }
        else
        {
            SDL.LogWarn(SDL.LogCategory.Application, "App state pointer was null during Quit.");
        }

        // SDL.Quit() is called automatically by SDL after this function returns.
        SDL.LogInfo(SDL.LogCategory.Application, "Quit Callback Complete.");
    }


    // --- Application Entry Point ---

    [STAThread]
    private static int Main(string[] args)
    {
        // Create delegates pointing to our callback methods.
        // IMPORTANT: These delegates MUST be kept alive for the duration of EnterAppMainCallbacks.
        // Storing them in local variables here is sufficient because EnterAppMainCallbacks blocks.
        SDL.AppInitFunc initFunc = MyAppInit;
        SDL.AppIterateFunc iterateFunc = MyAppIterate;
        SDL.AppEventFunc eventFunc = MyAppEvent;
        SDL.AppQuitFunc quitFunc = MyAppQuit;

        // Enter the SDL main loop managed by the callbacks.
        // This function will block until one of the callbacks
        // returns Success or Failure.
        SDL.LogInfo(SDL.LogCategory.Application, "Entering SDL Main Callbacks...");
        int exitCode = SDL.EnterAppMainCallbacks(args.Length, args, initFunc, iterateFunc, eventFunc, quitFunc);
        SDL.LogInfo(SDL.LogCategory.Application, $"Exited SDL Main Callbacks with code: {exitCode}");

        return exitCode; // Return the exit code SDL determined.
    }
}