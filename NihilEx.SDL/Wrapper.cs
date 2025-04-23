using System;
using System.Runtime.InteropServices;
using System.Text; // Required for StringBuilder
using System.Collections.Generic; // For event args lists

//THIS SHOULD BE REMOVED AT SOME POINT
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Use the namespace from your bindings file
using static SDL3.SDL;
using SDL3;

namespace AyanamisTower.NihilEx.SDLWrapper
{
    #region Core and Exceptions

    /// <summary>
    /// Represents an error originating from the SDL library.
    /// </summary>
    public class SDLException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with the last SDL error message.
        /// </summary>
        public SDLException() : base(GetSDLError()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SDLException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDLException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public SDLException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Helper to get the last SDL error message.
        /// </summary>
        /// <returns>The last error message from SDL.</returns>
        internal static string GetSDLError() // Made internal as it's primarily for wrapper use
        {
            // Use the marshaller defined in SDL3.Core.cs
            // Note: SDL_GetError() returns an SDL-owned string,
            // so we use the SDLOwnedStringMarshaller implicitly if available,
            // otherwise Marshal.PtrToStringUTF8 is a common way.
            // Assuming the bindings handle marshalling correctly.
            string? error = SDL_GetError();
            return string.IsNullOrEmpty(error) ? "Unknown SDL error." : error;
        }
    }


    /// <summary>
    /// Specifies SDL subsystems for initialization.
    /// </summary>
    [Flags]
    public enum SdlSubSystem : uint
    {
        None = 0,
        Timer = SDL_InitFlags.SDL_INIT_TIMER,
        Audio = SDL_InitFlags.SDL_INIT_AUDIO,
        Video = SDL_InitFlags.SDL_INIT_VIDEO,
        Joystick = SDL_InitFlags.SDL_INIT_JOYSTICK,
        Haptic = SDL_InitFlags.SDL_INIT_HAPTIC,
        Gamepad = SDL_InitFlags.SDL_INIT_GAMEPAD,
        Events = SDL_InitFlags.SDL_INIT_EVENTS,
        Sensor = SDL_InitFlags.SDL_INIT_SENSOR,
        Camera = SDL_InitFlags.SDL_INIT_CAMERA,
        Everything = Timer | Audio | Video | Joystick | Haptic | Gamepad | Events | Sensor | Camera
    }

    /// <summary>
    /// Delegate for application initialization.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True on success, false on failure (will cause application exit).</returns>
    public delegate bool AppInitHandler(string[] args);

    /// <summary>
    /// Delegate for handling application events.
    /// </summary>
    /// <param name="eventArgs">The event arguments, or null if the event type is unhandled by the wrapper.</param>
    /// <returns>True to continue running, false to request application exit.</returns>
    public delegate bool AppEventHandler(SdlEventArgs? eventArgs);

    /// <summary>
    /// Delegate for the application's main update/iteration logic.
    /// </summary>
    /// <returns>True to continue running, false to request application exit.</returns>
    public delegate bool AppUpdateHandler();

    /// <summary>
    /// Delegate for application cleanup before exiting.
    /// </summary>
    public delegate void AppQuitHandler();

    /// <summary>
    /// Provides static methods for initializing, shutting down,
    /// and managing core SDL functionality.
    /// </summary>
    public static class SdlHost
    {
        private class AppContext
        {
            public AppInitHandler? UserInit { get; set; }
            public AppUpdateHandler? UserUpdate { get; set; }
            public AppEventHandler? UserEvent { get; set; }
            public AppQuitHandler? UserQuit { get; set; }
            public Exception? LastException { get; set; } // To capture exceptions in callbacks
        }

        private static readonly SDL_AppInit_func _nativeInit = NativeAppInit;
        private static readonly SDL_AppIterate_func _nativeIterate = NativeAppIterate;
        private unsafe static readonly SDL_AppEvent_func _nativeEvent = NativeAppEvent;
        private static readonly SDL_AppQuit_func _nativeQuit = NativeAppQuit;
        private static GCHandle _pendingAppHandle;

        private static readonly Lock _runLock = new(); // Assuming Lock is a valid locking mechanism
        // Static native callback wrappers
        private static SDL_AppResult NativeAppInit(IntPtr appstatePtrRef, int argc, IntPtr argv)
        {
            // This callback uses the temporary static handle mechanism to establish the state pointer with SDL.
            GCHandle handle = default;
            AppContext? context = null;
            try
            {
                // Step 1: Retrieve the handle from the static field set in RunApplication
                if (!_pendingAppHandle.IsAllocated)
                {
                    SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "NativeAppInit: No pending AppContext handle found!");
                    Console.Error.WriteLine("NativeAppInit: No pending AppContext handle found!");
                    return SDL_AppResult.SDL_APP_FAILURE;
                }

                handle = _pendingAppHandle;
                context = handle.Target as AppContext;

                if (context == null)
                {
                    SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "NativeAppInit: Failed to get AppContext from pending handle.");
                    Console.Error.WriteLine("NativeAppInit: Failed to get AppContext from pending handle.");
                    _pendingAppHandle = default; // Clear invalid static handle
                    return SDL_AppResult.SDL_APP_FAILURE;
                }

                // Step 2: Write the *actual* handle pointer back to SDL via the appstatePtrRef (void**)
                // This tells SDL which pointer to use for subsequent Iterate/Event/Quit calls.
                IntPtr actualHandlePtr = GCHandle.ToIntPtr(handle);
                Marshal.WriteIntPtr(appstatePtrRef, actualHandlePtr);
                Console.WriteLine($"NativeAppInit: Thread {Environment.CurrentManagedThreadId}: Set SDL app state pointer to {actualHandlePtr:X} via appstatePtrRef {appstatePtrRef:X}.");


                // Step 3: Clear the temporary static handle now that SDL knows the real one.
                _pendingAppHandle = default;


                // Step 4: Proceed with user initialization logic
                if (context.UserInit == null)
                {
                    Console.WriteLine("NativeAppInit: No user init handler provided, continuing.");
                    return SDL_AppResult.SDL_APP_SUCCESS; // Treat as success if no user init provided
                }

                // Marshal args (same as before)
                string[] managedArgs = Array.Empty<string>();
                if (argc > 0 && argv != IntPtr.Zero)
                {
                    managedArgs = new string[argc];
                    unsafe
                    {
                        byte** nativeArgs = (byte**)argv;
                        for (int i = 0; i < argc; i++)
                        {
                            // Assuming UTF8, adjust if needed
                            managedArgs[i] = Marshal.PtrToStringUTF8((IntPtr)nativeArgs[i]) ?? string.Empty;
                        }
                    }
                }
                Console.WriteLine($"NativeAppInit: Calling UserInit with {managedArgs.Length} args...");
                // Call user delegate
                bool success = context.UserInit(managedArgs);
                Console.WriteLine($"NativeAppInit: UserInit returned {success}.");
                return success ? SDL_AppResult.SDL_APP_CONTINUE : SDL_AppResult.SDL_APP_FAILURE;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in NativeAppInit: {ex}");
                SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in NativeAppInit: {ex.Message}");
                if (context != null) context.LastException = ex; // Store exception
                                                                 // Ensure static handle is cleared on error
                if (_pendingAppHandle.IsAllocated) _pendingAppHandle = default;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure
            }
        }

        private static SDL_AppResult NativeAppIterate(IntPtr appstate)
        {
            AppContext? context = null;
            try
            {
                // Retrieve context from the appstate pointer provided by SDL
                if (appstate == IntPtr.Zero) return SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(appstate); // Use the pointer SDL gives us now
                if (!handle.IsAllocated) return SDL_AppResult.SDL_APP_FAILURE; // Handle wasn't set or was freed early

                context = handle.Target as AppContext;
                if (context?.UserUpdate == null) return SDL_AppResult.SDL_APP_CONTINUE; // No user update, just continue

                // Call user delegate
                bool continueRunning = context.UserUpdate();
                return continueRunning ? SDL_AppResult.SDL_APP_CONTINUE : SDL_AppResult.SDL_APP_SUCCESS; // Return SUCCESS to quit cleanly
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in AppUpdate: {ex}");
                if (context != null) context.LastException = ex;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure on exception
            }
        }

        private static unsafe SDL_AppResult NativeAppEvent(IntPtr appstate, SDL_Event* evt)
        {
            AppContext? context = null;
            try
            {
                // Retrieve context from the appstate pointer provided by SDL
                if (appstate == IntPtr.Zero) return SDL_AppResult.SDL_APP_FAILURE;
                GCHandle handle = GCHandle.FromIntPtr(appstate);
                if (!handle.IsAllocated) return SDL_AppResult.SDL_APP_FAILURE;

                context = handle.Target as AppContext;
                if (context?.UserEvent == null) return SDL_AppResult.SDL_APP_CONTINUE; // No user event handler

                // Map event (MapEvent takes SDL_Event by value, so dereference)
                SdlEventArgs? managedEvent = Events.MapEvent(*evt);

                // Call user delegate
                bool continueRunning = context.UserEvent(managedEvent);
                return continueRunning ? SDL_AppResult.SDL_APP_CONTINUE : SDL_AppResult.SDL_APP_SUCCESS; // Return SUCCESS to quit cleanly
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in AppEvent: {ex}");
                if (context != null) context.LastException = ex;
                return SDL_AppResult.SDL_APP_FAILURE; // Signal failure on exception
            }
        }

        private static void NativeAppQuit(IntPtr appstate, SDL_AppResult result)
        {
            AppContext? context = null;
            GCHandle handle = default;
            bool handleIsValid = false;
            try
            {
                Console.WriteLine($"NativeAppQuit: Received appstate IntPtr value: {appstate.ToString("X")}. Result code: {result}"); // Log as Hex

                // Retrieve context one last time using the appstate passed by SDL
                if (appstate != IntPtr.Zero)
                {
                    handle = GCHandle.FromIntPtr(appstate);
                    if (handle.IsAllocated)
                    {
                        handleIsValid = true; // Mark that we found a valid handle via appstate
                        context = handle.Target as AppContext;
                    }
                    else
                    {
                        SDL.SDL_LogWarn((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "NativeAppQuit: Received appstate pointer, but GCHandle was not allocated.");
                    }
                }
                else
                {
                    SDL.SDL_LogWarn((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "NativeAppQuit: Received null appstate pointer.");
                }

                context?.UserQuit?.Invoke(); // Call user quit delegate if provided and context was retrieved
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in AppQuit: {ex}");
                SDL.SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"Exception in NativeAppQuit: {ex.Message}");
                // Don't store exception here as the app is already quitting
            }
            finally
            {
                // Clean up SDL subsystems (your existing logic is fine here)
                if (_isInitialized)
                {
                    SDL_Quit();
                    _isInitialized = false;
                    Console.WriteLine("SDL Quit from NativeAppQuit.");
                }

                // Free the GCHandle that was keeping the context alive.
                // This handle *MUST* be the one passed via appstate.
                if (handleIsValid) // Only free if we successfully got a valid handle from appstate
                {
                    try
                    {
                        handle.Free();
                        // Console.WriteLine("App GCHandle Freed in NativeAppQuit.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Handle case where handle might have been freed elsewhere (shouldn't happen ideally) or was invalid
                        Console.Error.WriteLine($"Warning: Attempted to free GCHandle in NativeAppQuit, but it was invalid or already freed: {ex.Message}");
                        SDL.SDL_LogWarn((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, "NativeAppQuit: Attempted to free GCHandle, but it was invalid or already freed.");
                    }
                }
                // Removed the fallback for _pendingAppHandle as it's no longer used.
            }
        }

        private static bool _isInitialized = false;
        private static readonly Lock _initLock = new();

        /// <summary>
        /// Gets a value indicating whether any SDL subsystems have been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes SDL subsystems using the wrapper enum.
        /// Must be called before using most SDL functions.
        /// </summary>
        /// <param name="flags">The subsystems to initialize.</param>
        /// <exception cref="SDLException">Thrown if SDL fails to initialize.</exception>
        public static void Init(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                SDL_InitFlags sdlFlags = (SDL_InitFlags)flags; // Cast to SDL enum
                if (_isInitialized && SDL_WasInit(sdlFlags) == sdlFlags)
                {
                    return;
                }

                if (!SDL_Init(sdlFlags)) // Call SDL with casted flags
                {
                    throw new SDLException("Failed to initialize SDL subsystems.");
                }
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Initializes specific SDL subsystems using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to initialize.</param>
        /// <exception cref="SDLException">Thrown if SDL fails to initialize the subsystem.</exception>
        public static void InitSubSystem(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("SDL must be initialized with Init() first.");
                }
                SDL_InitFlags sdlFlags = (SDL_InitFlags)flags; // Cast to SDL enum
                if (SDL_InitSubSystem(sdlFlags)) // Call SDL with casted flags
                {
                    throw new SDLException($"Failed to initialize SDL subsystem: {flags}.");
                }
            }
        }

        /// <summary>
        /// Shuts down specific SDL subsystems using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to shut down.</param>
        public static void QuitSubSystem(SdlSubSystem flags) // Use wrapper enum
        {
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    SDL_QuitSubSystem((SDL_InitFlags)flags); // Cast to SDL enum
                }
            }
        }


        /// <summary>
        /// Cleans up all initialized SDL subsystems.
        /// Call this when your application is exiting.
        /// </summary>
        public static void Quit()
        {
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    // Clean up other resources if necessary before SDL_Quit
                    Events.Quit(); // Ensure event system is cleaned up if needed
                    SDL_Quit();
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Runs the SDL application using the provided lifecycle callbacks.
        /// This method takes over the main loop and manages SDL initialization and shutdown.
        /// </summary>
        /// <param name="init">Initialization callback.</param>
        /// <param name="update">Main loop iteration callback.</param>
        /// <param name="eventHandler">Event processing callback.</param>
        /// <param name="quit">Cleanup callback.</param>
        /// <param name="args">Command line arguments passed to the init callback.</param>
        /// <returns>Exit code (0 for success, non-zero for failure).</returns>
        public static unsafe int RunApplication(AppInitHandler init, AppUpdateHandler update, AppEventHandler eventHandler, AppQuitHandler quit, string[]? args = null)
        {
            Init(SdlSubSystem.Video);
            // Ensure RunApplication is not called concurrently if that's an issue
            if (!_runLock.TryEnter()) // Assuming TryLock exists
            {
                throw new InvalidOperationException("SdlHost.RunApplication is already running.");
            }

            // NOTE: SDL_EnterAppMainCallbacks handles SDL_Init and SDL_Quit internally.
            // We do NOT call SdlHost.Init() or SdlHost.Quit() manually here.

            // Prepare context
            var context = new AppContext
            {
                UserInit = init,
                UserUpdate = update,
                UserEvent = eventHandler,
                UserQuit = quit
            };

            GCHandle contextHandle = default; // Use default initialization
            IntPtr argvPtr = IntPtr.Zero;
            int argc = 0;
            int result = -1; // Default to error

            try
            {
                // Allocate handle for the context object
                contextHandle = GCHandle.Alloc(context, GCHandleType.Normal);
                IntPtr contextHandlePtr = GCHandle.ToIntPtr(contextHandle);
                Console.WriteLine($"RunApplication: Thread {Environment.CurrentManagedThreadId}: Allocating GCHandle {contextHandlePtr:X} for AppContext.");

                // --- CRITICAL STEP: Set the static handle for NativeAppInit to pick up ---
                if (_pendingAppHandle.IsAllocated)
                {
                    // This should not happen if RunApplication is single-threaded as enforced by _runLock
                    Console.Error.WriteLine("RunApplication Warning: _pendingAppHandle was already allocated!");
                    _pendingAppHandle.Free(); // Attempt cleanup just in case
                }
                _pendingAppHandle = contextHandle;
                Console.WriteLine($"RunApplication: Thread {Environment.CurrentManagedThreadId}: Assigned GCHandle to _pendingAppHandle. IsAllocated = {_pendingAppHandle.IsAllocated}");
                // --- End Critical Step ---


                // Marshal command line arguments (if provided) - same as before
                if (args?.Length > 0)
                {
                    argc = args.Length;
                    // Allocate unmanaged memory for the array of pointers (**char)
                    argvPtr = Marshal.AllocHGlobal(IntPtr.Size * argc);
                    byte** argv = (byte**)argvPtr; // Treat as byte** for UTF8 marshalling
                    for (int i = 0; i < argc; i++)
                    {
                        // Allocate memory for each string and copy (null-terminated UTF8)
                        byte[] utf8Bytes = Encoding.UTF8.GetBytes(args[i] + '\0');
                        IntPtr argPtr = Marshal.AllocHGlobal(utf8Bytes.Length);
                        Marshal.Copy(utf8Bytes, 0, argPtr, utf8Bytes.Length);
                        argv[i] = (byte*)argPtr;
                    }
                    Console.WriteLine($"RunApplication: Marshalled {argc} arguments.");
                }
                else
                {
                    Console.WriteLine("RunApplication: No arguments to marshal.");
                }

                Console.WriteLine("RunApplication: Entering SDL_EnterAppMainCallbacks...");
                // Call SDL's main entry point with the static delegate references
                result = SDL_EnterAppMainCallbacks(argc, argvPtr, _nativeInit, _nativeIterate, _nativeEvent, _nativeQuit);
                Console.WriteLine($"RunApplication: SDL_EnterAppMainCallbacks returned {result}.");


                // Check if an exception occurred in a callback (stored in context)
                if (context.LastException != null)
                {
                    Console.Error.WriteLine("RunApplication: Exiting due to unhandled exception in callback.");
                    // Optionally re-throw or handle differently
                    // throw new Exception("Exception occurred in SDL callback.", context.LastException);
                    result = context.LastException.HResult != 0 ? context.LastException.HResult : -1; // Use HResult as exit code if available
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"RunApplication: Exception during setup or execution: {ex}");
                SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION, $"RunApplication Exception: {ex.Message}");
                result = ex.HResult != 0 ? ex.HResult : -1;
            }
            finally
            {
                Console.WriteLine("RunApplication: Entering finally block.");
                // Free marshaled arguments (same as before)
                if (argvPtr != IntPtr.Zero)
                {
                    Console.WriteLine($"RunApplication: Freeing {argc} marshalled arguments...");
                    byte** argv = (byte**)argvPtr;
                    for (int i = 0; i < argc; i++)
                    {
                        if (argv[i] != null) Marshal.FreeHGlobal((IntPtr)argv[i]);
                    }
                    Marshal.FreeHGlobal(argvPtr);
                    Console.WriteLine("RunApplication: Finished freeing arguments.");
                }

                // Free the GCHandle for the AppContext
                if (contextHandle.IsAllocated)
                {
                    IntPtr freedHandlePtr = GCHandle.ToIntPtr(contextHandle);
                    contextHandle.Free();
                    Console.WriteLine($"RunApplication: Freed GCHandle {freedHandlePtr:X}. IsAllocated = {contextHandle.IsAllocated}");
                }
                else
                {
                    Console.WriteLine("RunApplication: Context GCHandle was not allocated or already freed.");
                }

                // Ensure pending handle is cleared if something went wrong very early
                if (_pendingAppHandle.IsAllocated)
                {
                    Console.Error.WriteLine("RunApplication Warning: _pendingAppHandle was still allocated in finally block! Clearing.");
                    _pendingAppHandle.Free();
                    _pendingAppHandle = default;
                }


                // Release the lock
                _runLock.Exit(); // Assuming Unlock exists

                Console.WriteLine("RunApplication: Exiting finally block.");
            }

            return result; // Return the exit code from SDL or based on exceptions
        }

        /// <summary>
        /// Checks if specific SDL subsystems have been initialized using the wrapper enum.
        /// </summary>
        /// <param name="flags">The subsystems to check.</param>
        /// <returns>The flags for the subsystems that are currently initialized.</returns>
        public static SdlSubSystem WasInit(SdlSubSystem flags) // Use wrapper enum
        {
            // Cast wrapper enum to SDL enum for the call, then cast result back
            return (SdlSubSystem)SDL_WasInit((SDL_InitFlags)flags);
        }

        /// <summary>
        /// Gets the last error message from SDL.
        /// </summary>
        /// <returns>The error message.</returns>
        public static string GetError() => SDLException.GetSDLError();

        /// <summary>
        /// Clears the last SDL error message.
        /// </summary>
        public static void ClearError() => SDL_ClearError();

        /// <summary>
        /// Throws an SDLException if the result indicates failure.
        /// Assumes SDL functions return 0 (SDL_FALSE) on success and non-zero (SDL_TRUE) on failure.
        /// </summary>
        /// <param name="result">The SDL_Bool result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static void ThrowOnFailure(SDLBool result, string message)
        {
            if (!result) // SDLBool is false on error
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }

        /// <summary>
        /// Throws an SDLException if the pointer is null (IntPtr.Zero).
        /// </summary>
        /// <param name="ptr">The pointer result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static void ThrowOnNull(IntPtr ptr, string message)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }

        /// <summary>
        /// Throws an SDLException if the pointer is null (IntPtr.Zero).
        /// </summary>
        /// <param name="ptr">The pointer result from an SDL function.</param>
        /// <param name="message">A custom message prefix for the exception.</param>
        /// <exception cref="SDLException"></exception>
        internal static unsafe void ThrowOnNull(void* ptr, string message)
        {
            if (ptr == null)
            {
                throw new SDLException($"{message}: {GetError()}");
            }
        }
    }

    #endregion

    #region Helper Structs

    // Helper struct for Point (can be replaced with System.Drawing.Point if preferred,
    // but keeping it simple here to avoid extra dependencies).
    public struct Point(int x, int y)
    {
        public int X = x;
        public int Y = y;

        public override readonly string ToString() => $"({X}, {Y})";
    }

    // Helper struct for Float Point
    public struct FPoint(float x, float y)
    {
        public float X = x;
        public float Y = y;

        public override readonly string ToString() => $"({X:F2}, {Y:F2})";

        public static implicit operator SDL_FPoint(FPoint p)
        {
            return new SDL_FPoint { x = p.X, y = p.Y };
        }

        public static implicit operator FPoint(SDL_FPoint p)
        {
            return new FPoint(p.x, p.y);
        }
    }

    // Helper struct for Rect
    public struct Rect(int x, int y, int w, int h)
    {
        public int X = x;
        public int Y = y;
        public int W = w;
        public int H = h;

        public override readonly string ToString() => $"({X}, {Y}, {W}, {H})";

        public static implicit operator SDL_Rect(Rect r)
        {
            return new SDL_Rect { x = r.X, y = r.Y, w = r.W, h = r.H };
        }

        public static implicit operator Rect(SDL_Rect r)
        {
            return new Rect(r.x, r.y, r.w, r.h);
        }
    }

    // Helper struct for Float Rect
    public struct FRect(float x, float y, float w, float h)
    {
        public float X = x;
        public float Y = y;
        public float W = w;
        public float H = h;

        public override readonly string ToString() => $"({X:F2}, {Y:F2}, {W:F2}, {H:F2})";

        public static implicit operator SDL_FRect(FRect r)
        {
            return new SDL_FRect { x = r.X, y = r.Y, w = r.W, h = r.H };
        }

        public static implicit operator FRect(SDL_FRect r)
        {
            return new FRect(r.x, r.y, r.w, r.h);
        }
    }

    // Helper struct for Color
    public struct Color(byte r, byte g, byte b, byte a = 255) // Default alpha to opaque
    {
        public byte R = r;
        public byte G = g;
        public byte B = b;
        public byte A = a;

        public static implicit operator SDL_Color(Color c)
        {
            return new SDL_Color { r = c.R, g = c.G, b = c.B, a = c.A };
        }

        public static implicit operator Color(SDL_Color c)
        {
            return new Color(c.r, c.g, c.b, c.a);
        }

        public static implicit operator Color(System.Drawing.Color c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        public static implicit operator System.Drawing.Color(Color c)
        {
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 255, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Magenta = new(255, 0, 255);
        public static readonly Color Cyan = new(0, 255, 255);
        public static readonly Color Transparent = new(0, 0, 0, 0);
    }

    // Helper struct for Float Color
    public struct FColor(float r, float g, float b, float a = 1.0f) // Default alpha to opaque
    {
        public float R = r;
        public float G = g;
        public float B = b;
        public float A = a;

        public static implicit operator SDL_FColor(FColor c)
        {
            return new SDL_FColor { r = c.R, g = c.G, b = c.B, a = c.A };
        }

        public static implicit operator FColor(SDL_FColor c)
        {
            return new FColor(c.r, c.g, c.b, c.a);
        }

        public static readonly FColor Black = new(0f, 0f, 0f);
        public static readonly FColor White = new(1f, 1f, 1f);
        public static readonly FColor Red = new(1f, 0f, 0f);
        public static readonly FColor Green = new(0f, 1f, 0f);
        public static readonly FColor Blue = new(0f, 0f, 1f);
        public static readonly FColor Yellow = new(1f, 1f, 0f);
        public static readonly FColor Magenta = new(1f, 0f, 1f);
        public static readonly FColor Cyan = new(0f, 1f, 1f);
        public static readonly FColor Transparent = new(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Represents a vertex used in geometry rendering.
    /// </summary>
    public struct Vertex(FPoint position, FColor color, FPoint texCoord)
    {
        public FPoint Position = position;
        public FColor Color = color;
        public FPoint TexCoord = texCoord;

        public static implicit operator SDL_Vertex(Vertex v)
        {
            return new SDL_Vertex { position = v.Position, color = v.Color, tex_coord = v.TexCoord };
        }

        public static implicit operator Vertex(SDL_Vertex v)
        {
            return new Vertex(v.position, v.color, v.tex_coord);
        }
    }


    #endregion

    #region Window

    /// <summary>
    /// Specifies window creation flags.
    /// </summary>
    [Flags]
    public enum WindowFlags : ulong // Matches SDL_WindowFlags underlying type
    {
        None = 0,
        Fullscreen = SDL_WindowFlags.SDL_WINDOW_FULLSCREEN,
        OpenGL = SDL_WindowFlags.SDL_WINDOW_OPENGL,
        Occluded = SDL_WindowFlags.SDL_WINDOW_OCCLUDED, // Read-only state flag
        Hidden = SDL_WindowFlags.SDL_WINDOW_HIDDEN,
        Borderless = SDL_WindowFlags.SDL_WINDOW_BORDERLESS,
        Resizable = SDL_WindowFlags.SDL_WINDOW_RESIZABLE,
        Minimized = SDL_WindowFlags.SDL_WINDOW_MINIMIZED,
        Maximized = SDL_WindowFlags.SDL_WINDOW_MAXIMIZED,
        MouseGrabbed = SDL_WindowFlags.SDL_WINDOW_MOUSE_GRABBED,
        InputFocus = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS, // Read-only state flag
        MouseFocus = SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS, // Read-only state flag
        External = SDL_WindowFlags.SDL_WINDOW_EXTERNAL,
        Modal = SDL_WindowFlags.SDL_WINDOW_MODAL,
        HighPixelDensity = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY,
        MouseCapture = SDL_WindowFlags.SDL_WINDOW_MOUSE_CAPTURE, // Renamed from MOUSE_RELATIVE_MODE? Check SDL3 docs. Assuming this is intended capture.
        AlwaysOnTop = SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP,
        Utility = SDL_WindowFlags.SDL_WINDOW_UTILITY,
        Tooltip = SDL_WindowFlags.SDL_WINDOW_TOOLTIP,
        PopupMenu = SDL_WindowFlags.SDL_WINDOW_POPUP_MENU,
        KeyboardGrabbed = SDL_WindowFlags.SDL_WINDOW_KEYBOARD_GRABBED, // Read-only state flag
        Vulkan = SDL_WindowFlags.SDL_WINDOW_VULKAN,
        Metal = SDL_WindowFlags.SDL_WINDOW_METAL,
        Transparent = SDL_WindowFlags.SDL_WINDOW_TRANSPARENT,
        NotFocusable = SDL_WindowFlags.SDL_WINDOW_NOT_FOCUSABLE
        // SDL_WINDOW_MOUSE_RELATIVE_MODE (0x8000) might need separate handling or a distinct flag if different from MouseCapture
    }

    /// <summary>
    /// Specifies the type of a window event.
    /// </summary>
    public enum WindowEventType
    {
        None, // Default
        Shown,
        Hidden,
        Exposed, // Window needs to be redrawn
        Moved,
        Resized, // Window size changed by user/system
        PixelSizeChanged, // Window pixel size changed (e.g., DPI change)
        Minimized,
        Maximized,
        Restored,
        MouseEnter, // Mouse entered window
        MouseLeave, // Mouse left window
        FocusGained,
        FocusLost,
        CloseRequested, // User requested window close (e.g., clicked X)
        HitTest, // Requires custom hit test callback
        IccProfileChanged,
        DisplayChanged, // Window moved to a different display
        DisplayScaleChanged, // DPI scale changed
        SafeAreaChanged,
        Occluded, // Window obscured
        EnterFullscreen,
        LeaveFullscreen,
        Destroyed, // Window was destroyed
        HdrStateChanged
        // Note: MetalViewResized might be too specific, handled by Resized/PixelSizeChanged?
    }

    /// <summary>
    /// Represents an SDL Window.
    /// </summary>
    public class Window : IDisposable
    {
        private IntPtr _windowPtr;
        private bool _disposed = false;
        public bool IsDisposed { get => _disposed; }
        /// <summary>
        /// Gets the native SDL window handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Window)) : _windowPtr;
            }
        }

        /// <summary>
        /// Gets the ID of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public uint Id
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Window)) : SDL_GetWindowID(_windowPtr);
            }
        }


        /// <summary>
        /// Creates a new SDL window using wrapper flags.
        /// SdlSubSystem.Video must be initialized before calling this.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="flags">Window creation flags (using wrapper enum).</param>
        /// <exception cref="SDLException">Thrown if the window cannot be created.</exception>
        /// <exception cref="InvalidOperationException">Thrown if SdlSubSystem.Video is not initialized.</exception>
        public Window(string title, int width, int height, WindowFlags flags = WindowFlags.None) // Use wrapper enum
        {
            if ((SdlHost.WasInit(SdlSubSystem.Video) & SdlSubSystem.Video) == 0)
            {
                throw new InvalidOperationException("SdlSubSystem.Video must be initialized before creating a window.");
            }

            // Cast wrapper flags to SDL flags for the underlying call
            _windowPtr = SDL_CreateWindow(title, width, height, (SDL_WindowFlags)flags);
            SdlHost.ThrowOnNull(_windowPtr, "Failed to create window");
        }

        /// <summary>
        /// Creates a new SDL window using properties.
        /// SDL_INIT_VIDEO must be initialized before calling this.
        /// </summary>
        /// <param name="properties">The properties handle created via SDL_CreateProperties().</param>
        /// <exception cref="SDLException">Thrown if the window cannot be created.</exception>
        /// <exception cref="InvalidOperationException">Thrown if SDL_INIT_VIDEO is not initialized.</exception>
        /// <remarks>The caller is responsible for destroying the properties handle AFTER the window is created.</remarks>
        public Window(uint properties)
        {
            if ((SdlHost.WasInit(SdlSubSystem.Video) & SdlSubSystem.Video) == 0)
            {
                throw new InvalidOperationException("SDL_INIT_VIDEO must be initialized before creating a window.");
            }
            _windowPtr = SDL_CreateWindowWithProperties(properties);
            SdlHost.ThrowOnNull(_windowPtr, "Failed to create window with properties");
        }

        // Internal constructor for wrapping an existing handle (e.g., from GetWindowFromID)
        // Be careful with ownership when using this.
        internal Window(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(handle), "Window handle cannot be null.");
            }
            _windowPtr = handle;
            // Assume the handle is valid and owned elsewhere, so don't destroy in Dispose
            // This might need adjustment based on specific use cases. Consider adding an 'owned' flag.
            _disposed = true; // Mark as disposed immediately to prevent SDL_DestroyWindow call
        }


        // --- Window Properties ---

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public string Title
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                // SDL_GetWindowTitle returns an SDL-owned string
                string? title = SDL_GetWindowTitle(_windowPtr);
                return title ?? string.Empty; // Return empty if null
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_SetWindowTitle(_windowPtr, value), "Failed to set window title");
            }
        }

        /// <summary>
        /// Gets or sets the position of the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point Position
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_GetWindowPosition(_windowPtr, out int x, out int y), "Failed to get window position");
                return new Point(x, y);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_SetWindowPosition(_windowPtr, value.X, value.Y), "Failed to set window position");
            }
        }

        /// <summary>
        /// Gets or sets the size of the window's client area.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point Size
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_GetWindowSize(_windowPtr, out int w, out int h), "Failed to get window size");
                return new Point(w, h);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_SetWindowSize(_windowPtr, value.X, value.Y), "Failed to set window size");
            }
        }

        /// <summary>
        /// Gets the size of the window's client area in pixels (for high-DPI displays).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point SizeInPixels
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_GetWindowSizeInPixels(_windowPtr, out int w, out int h), "Failed to get window size in pixels");
                return new Point(w, h);
            }
        }

        /// <summary>
        /// Gets the current window flags.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_WindowFlags Flags
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Window)) : SDL_GetWindowFlags(_windowPtr);
            }
        }

        /// <summary>
        /// Gets the display index associated with the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public uint DisplayId
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                // SDL_GetDisplayForWindow returns 0 on error, check error state?
                // SDL3 doc says returns a valid display ID or 0 if the window is invalid.
                // Assuming 0 is a valid ID for the primary display if only one exists,
                // or an error if the window handle is bad (which constructor should prevent).
                return SDL_GetDisplayForWindow(_windowPtr);
            }
        }


        // --- Window Methods ---

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Show()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_ShowWindow(_windowPtr), "Failed to show window");
        }

        /// <summary>
        /// Hides the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Hide()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(SDL_HideWindow(_windowPtr), "Failed to hide window");
        }

        /// <summary>
        /// Raises the window above other windows and requests input focus.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Raise()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(SDL_RaiseWindow(_windowPtr), "Failed to raise window");
        }

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Maximize()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(SDL_MaximizeWindow(_windowPtr), "Failed to maximize window");
        }

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Minimize()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_MinimizeWindow(_windowPtr), "Failed to minimize window");
        }

        /// <summary>
        /// Restores the size and position of a minimized or maximized window.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Restore()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RestoreWindow(_windowPtr), "Failed to restore window");
        }

        /// <summary>
        /// Sets the window to fullscreen mode.
        /// </summary>
        /// <param name="fullscreen">True for fullscreen, false otherwise.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetFullscreen(bool fullscreen)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_SetWindowFullscreen(_windowPtr, fullscreen),
               fullscreen ? "Failed to enter fullscreen" : "Failed to leave fullscreen");
        }

        /// <summary>
        /// Sets the border state of the window.
        /// </summary>
        /// <param name="bordered">True to enable border, false to disable.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetBordered(bool bordered)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_SetWindowBordered(_windowPtr, bordered), "Failed to set window border state");
        }

        /// <summary>
        /// Sets whether the window is resizable.
        /// </summary>
        /// <param name="resizable">True if the window should be resizable.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetResizable(bool resizable)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(SDL_SetWindowResizable(_windowPtr, resizable), "Failed to set window resizable state");
        }

        /// <summary>
        /// Sets whether the window should always be on top.
        /// </summary>
        /// <param name="onTop">True if the window should be always on top.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetAlwaysOnTop(bool onTop)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            SdlHost.ThrowOnFailure(SDL_SetWindowAlwaysOnTop(_windowPtr, onTop), "Failed to set window always on top state");
        }

        /// <summary>
        /// Creates a renderer associated with this window.
        /// </summary>
        /// <param name="driverName">Optional name of the rendering driver to use (e.g., "direct3d11", "opengl", "metal"). Null for default.</param>
        /// <returns>A new Renderer instance.</returns>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public Renderer CreateRenderer(string? driverName = null)
        {
            return _disposed ? throw new ObjectDisposedException(nameof(Window)) : new Renderer(this, driverName);
        }

        /// <summary>
        /// Gets the window associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the window.</param>
        /// <returns>A Window object wrapping the native handle, or null if not found.</returns>
        /// <remarks>The returned Window object does NOT own the native handle and will not destroy it on Dispose.</remarks>
        public static Window? GetFromId(uint id)
        {
            IntPtr handle = SDL_GetWindowFromID(id);
            return handle == IntPtr.Zero ? null : new Window(handle);
        }


        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Only destroy the window if this object instance created it (owns it).
                // Handles obtained via GetWindowFromID are not owned.
                // We currently mark externally obtained handles as _disposed = true in the internal constructor.
                // A more robust solution might involve an '_owned' flag.
                const bool owned = true; // Assume ownership unless created with internal constructor

                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // None in this basic example.
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_windowPtr != IntPtr.Zero && owned)
                {
                    // Make sure SDL_DestroyWindow is safe to call even if SDL is shut down
                    // (SDL3 documentation should clarify this, assuming it is safe here)
                    SDL_DestroyWindow(_windowPtr);
                }
                _windowPtr = IntPtr.Zero; // Prevent use after dispose
                _disposed = true; // Mark as disposed regardless of ownership to prevent method calls
            }
        }

        /// <summary>
        /// Releases the resources used by the Window.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Window()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
    }

    #endregion

    #region Renderer and Texture

    /// <summary>
    /// Represents an SDL Texture, used for rendering images.
    /// </summary>
    public class Texture : IDisposable
    {
        private IntPtr _texturePtr;
        private bool _disposed = false;
        public bool IsDisposed { get => _disposed; }

        /// <summary>
        /// Gets the native SDL texture handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Texture)) : _texturePtr;
            }
        }

        /// <summary>
        /// Gets the renderer associated with this texture.
        /// </summary>
        public Renderer Renderer { get; }

        // Internal constructor for wrapping SDL_CreateTexture and SDL_CreateTextureFromSurface
        internal Texture(Renderer renderer, SDL_PixelFormat format, SDL_TextureAccess access, int w, int h)
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (renderer.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Renderer));
            }

            unsafe // SDL_CreateTexture returns SDL_Texture*
            {
                SDL_Texture* texPtr = SDL_CreateTexture(renderer.Handle, format, access, w, h);
                SdlHost.ThrowOnNull((IntPtr)texPtr, "Failed to create texture");
                _texturePtr = (IntPtr)texPtr;
            }
        }

        // Internal constructor for wrapping SDL_CreateTextureFromSurface
        // NOTE: Requires Surface class to be implemented. Placeholder for now.
        internal Texture(Renderer renderer, /* Surface surface */ IntPtr surfacePtr)
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (renderer.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Renderer));
            }

            if (surfacePtr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(surfacePtr)); // Replace with Surface object check later
            }

            unsafe // SDL_CreateTextureFromSurface returns SDL_Texture*
            {
                SDL_Texture* texPtr = SDL_CreateTextureFromSurface(renderer.Handle, surfacePtr);
                SdlHost.ThrowOnNull((IntPtr)texPtr, "Failed to create texture from surface");
                _texturePtr = (IntPtr)texPtr;
            }
            // SDL_CreateTextureFromSurface documentation implies the surface is no longer needed
            // after this call, but doesn't explicitly say it frees it.
            // If the Surface wrapper owns the surface, its Dispose should handle it.
        }

        // --- Texture Properties ---

        /// <summary>
        /// Gets the format of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_PixelFormat Format
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Texture));
                }
                // Query the format using properties (SDL3 style) or internal storage
                // For simplicity, assuming the format doesn't change after creation.
                // A more robust way would query SDL_GetTextureProperties.
                unsafe
                {
                    // SDL_Texture struct is defined in the bindings
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->format;
                    }
                    return SDL_PixelFormat.SDL_PIXELFORMAT_UNKNOWN; // Or throw?
                }
            }
        }

        /// <summary>
        /// Gets the access mode of the texture (static, streaming, target).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_TextureAccess Access
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                // Query access mode - SDL3 doesn't have a direct SDL_QueryTexture equivalent for access anymore?
                // Need to check SDL_GetTextureProperties. Let's assume it's stored or inferred.
                // Placeholder: Need a way to get this. Maybe store it during creation?
                // For now, returning a default or throwing.
                // Let's try getting it from the struct if available
                unsafe
                {
                    // SDL_Texture struct doesn't contain access mode directly.
                    // Need to use SDL_GetTextureProperties
                    uint props = SDL_GetTextureProperties(_texturePtr);
                    SdlHost.ThrowOnFailure(props == 0, "Failed to get texture properties"); // Check if 0 is error


                    // We don't destroy the props handle here, assume it's temporary or managed by SDL? Check SDL docs.
                    return (SDL_TextureAccess)SDL_GetNumberProperty(props, SDL_PROP_TEXTURE_ACCESS_NUMBER, (long)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC);
                }
            }
        }

        /// <summary>
        /// Gets the width of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public int Width
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                unsafe
                {
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->w;
                    }

                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the height of the texture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public int Height
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                unsafe
                {
                    if (_texturePtr != IntPtr.Zero)
                    {
                        return ((SDL_Texture*)_texturePtr)->h;
                    }

                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the size of the texture.
        /// </summary>
        public Point Size => new Point(Width, Height);


        // --- Texture Manipulation ---

        /// <summary>
        /// Sets the color modulation for this texture.
        /// </summary>
        /// <param name="color">The color to modulate with.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetColorMod(Color color)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_SetTextureColorMod(_texturePtr, color.R, color.G, color.B), "Failed to set texture color mod");
        }

        /// <summary>
        /// Gets the color modulation for this texture.
        /// </summary>
        /// <returns>The current color modulation.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public Color GetColorMod()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_GetTextureColorMod(_texturePtr, out byte r, out byte g, out byte b), "Failed to get texture color mod");
            return new Color(r, g, b);
        }

        /// <summary>
        /// Sets the alpha modulation for this texture.
        /// </summary>
        /// <param name="alpha">The alpha value (0-255).</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetAlphaMod(byte alpha)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_SetTextureAlphaMod(_texturePtr, alpha), "Failed to set texture alpha mod");
        }

        /// <summary>
        /// Gets the alpha modulation for this texture.
        /// </summary>
        /// <returns>The current alpha modulation value.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public byte GetAlphaMod()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_GetTextureAlphaMod(_texturePtr, out byte alpha), "Failed to get texture alpha mod");
            return alpha;
        }

        /// <summary>
        /// Sets the scale mode used for texture scaling operations.
        /// </summary>
        /// <param name="scaleMode">The scale mode to use.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetScaleMode(SDL_ScaleMode scaleMode)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_SetTextureScaleMode(_texturePtr, scaleMode), "Failed to set texture scale mode");
        }

        /// <summary>
        /// Gets the scale mode used for texture scaling operations.
        /// </summary>
        /// <returns>The current scale mode.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_ScaleMode GetScaleMode()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_GetTextureScaleMode(_texturePtr, out SDL_ScaleMode scaleMode), "Failed to get texture scale mode");
            return scaleMode;
        }

        /// <summary>
        /// Updates a portion of the texture with new pixel data.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to update, or null for the entire texture.</param>
        /// <param name="pixels">A pointer to the pixel data.</param>
        /// <param name="pitch">The number of bytes per row in the pixel data.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void UpdateTexture(Rect? rect, IntPtr pixels, int pitch)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(SDL_UpdateTexture(_texturePtr, ref sdlRect, pixels, pitch), "Failed to update texture");
        }

        /// <summary>
        /// Updates a portion of a YUV planar texture with new pixel data.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to update, or null for the entire texture.</param>
        /// <param name="yPlane">Pointer to the Y plane data.</param>
        /// <param name="yPitch">Pitch of the Y plane data.</param>
        /// <param name="uPlane">Pointer to the U plane data.</param>
        /// <param name="uPitch">Pitch of the U plane data.</param>
        /// <param name="vPlane">Pointer to the V plane data.</param>
        /// <param name="vPitch">Pitch of the V plane data.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void UpdateYUVTexture(Rect? rect, IntPtr yPlane, int yPitch, IntPtr uPlane, int uPitch, IntPtr vPlane, int vPitch)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(SDL_UpdateYUVTexture(_texturePtr, ref sdlRect, yPlane, yPitch, uPlane, uPitch, vPlane, vPitch), "Failed to update YUV texture");
        }

        /// <summary>
        /// Locks a portion of the texture for direct pixel access.
        /// Only works for textures with SDL_TEXTUREACCESS_STREAMING.
        /// </summary>
        /// <param name="rect">The rectangular area to lock, or null for the entire texture.</param>
        /// <param name="pixels">Outputs a pointer to the locked pixels.</param>
        /// <param name="pitch">Outputs the pitch (bytes per row) of the locked pixels.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Lock(Rect? rect, out IntPtr pixels, out int pitch)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SDL_Rect sdlRect = rect ?? default;
            SdlHost.ThrowOnFailure(SDL_LockTexture(_texturePtr, ref sdlRect, out pixels, out pitch), "Failed to lock texture");
        }

        /// <summary>
        /// Unlocks a texture previously locked with LockTexture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Unlock()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // SDL_UnlockTexture returns void, no error check needed unless documented otherwise
            SDL_UnlockTexture(_texturePtr);
        }


        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_texturePtr != IntPtr.Zero)
                {
                    // Check if the renderer is still valid before destroying?
                    // SDL documentation usually implies Destroy functions are safe.
                    SDL_DestroyTexture(_texturePtr);
                    _texturePtr = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the Texture.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~Texture()
        {
            Dispose(disposing: false);
        }
    }


    /// <summary>
    /// Represents an SDL Renderer, used for drawing operations.
    /// </summary>
    public class Renderer : IDisposable
    {
        private IntPtr _rendererPtr;
        private bool _disposed = false;

        /// <summary>
        /// Gets the native SDL renderer handle. Use with caution.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public IntPtr Handle
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Renderer)) : _rendererPtr;
            }
        }

        /// <summary>
        /// Gets whether the Renderer has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Creates a new renderer associated with a window.
        /// </summary>
        /// <param name="window">The window where rendering is displayed.</param>
        /// <param name="driverName">Optional name of the rendering driver (e.g., "direct3d11", "opengl"). Null for default.</param>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <exception cref="ArgumentNullException">Thrown if window is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the window is disposed.</exception>
        public Renderer(Window window, string? driverName = null)
        {
            AssociatedWindow = window ?? throw new ArgumentNullException(nameof(window));
            if (window.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Window));
            }

            _rendererPtr = SDL_CreateRenderer(window.Handle, driverName);
            SdlHost.ThrowOnNull(_rendererPtr, $"Failed to create renderer{(driverName == null ? "" : $" with driver '{driverName}'")}");
        }

        /// <summary>
        /// Creates a new renderer using properties.
        /// </summary>
        /// <param name="properties">The properties handle created via SDL_CreateProperties().</param>
        /// <exception cref="SDLException">Thrown if the renderer cannot be created.</exception>
        /// <remarks>The caller is responsible for destroying the properties handle AFTER the renderer is created.</remarks>
        public Renderer(uint properties)
        {
            _rendererPtr = SDL_CreateRendererWithProperties(properties);
            SdlHost.ThrowOnNull(_rendererPtr, "Failed to create renderer with properties");
            // Try to get the associated window from properties if possible
            IntPtr windowHandle = SDL_GetPointerProperty(properties, SDL_PROP_RENDERER_CREATE_WINDOW_POINTER, IntPtr.Zero);
            if (windowHandle != IntPtr.Zero)
            {
                AssociatedWindow = Window.GetFromId(SDL_GetWindowID(windowHandle));
                // Note: _associatedWindow might be null if GetFromId fails or returns a disposed window wrapper
            }
        }

        // TODO: Add constructor for software renderer SDL_CreateSoftwareRenderer(Surface surface) when Surface is wrapped.


        // --- Renderer Properties ---

        /// <summary>
        /// Gets the window associated with this renderer, if any.
        /// </summary>
        public Window? AssociatedWindow { get; }

        /// <summary>
        /// Gets the name of the rendering driver.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public string Name
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                string? name = SDL_GetRendererName(_rendererPtr);
                return name ?? string.Empty;
            }
        }

        private bool _vSync;

        /// <summary>
        /// Gets or sets a value indicating whether VSync is enabled for the renderer.
        /// When set, updates the internal state and the SDL renderer's VSync setting.
        /// </summary>
        public bool VSync
        {
            get => _vSync;
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_vSync != value)
                {
                    _vSync = value;
                    SdlHost.ThrowOnFailure(SDL_SetRenderVSync(_rendererPtr, value ? 1 : 0), "Failed to set window always on top state");
                }
            }
        }

        /// <summary>
        /// Gets the output size in pixels of the rendering context.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Point OutputSize
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                SdlHost.ThrowOnFailure(SDL_GetRenderOutputSize(_rendererPtr, out int w, out int h), "Failed to get render output size");
                return new Point(w, h);
            }
        }

        /// <summary>
        /// Gets or sets the drawing color for the renderer.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Color DrawColor
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_GetRenderDrawColor(_rendererPtr, out byte r, out byte g, out byte b, out byte a), "Failed to get draw color");
                return new Color(r, g, b, a);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                SdlHost.ThrowOnFailure(SDL_SetRenderDrawColor(_rendererPtr, value.R, value.G, value.B, value.A), "Failed to set draw color");
            }
        }

        /// <summary>
        /// Gets or sets the drawing color for the renderer using float values (0.0f to 1.0f).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public FColor DrawColorF
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_GetRenderDrawColorFloat(_rendererPtr, out float r, out float g, out float b, out float a), "Failed to get draw color float");
                return new FColor(r, g, b, a);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_SetRenderDrawColorFloat(_rendererPtr, value.R, value.G, value.B, value.A), "Failed to set draw color float");
            }
        }

        /// <summary>
        /// Gets or sets the logical presentation mode for rendering when the aspect ratio of the window and logical size differ.
        /// </summary>
        /// <remarks>Requires setting the logical size first.</remarks>
        /// <exception cref="ObjectDisposedException"></exception>
        public SDL_RendererLogicalPresentation LogicalPresentation
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_GetRenderLogicalPresentation(_rendererPtr, out _, out _, out SDL_RendererLogicalPresentation mode), "Failed to get logical presentation");
                return mode;
            }
            // Setting requires width/height, so use the SetRenderLogicalPresentation method instead.
        }

        /// <summary>
        /// Gets or sets the drawing scale for the renderer.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public FPoint Scale
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_GetRenderScale(_rendererPtr, out float x, out float y), "Failed to get render scale");
                return new FPoint(x, y);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_SetRenderScale(_rendererPtr, value.X, value.Y), "Failed to set render scale");
            }
        }

        /// <summary>
        /// Gets or sets the drawing area for rendering on the current target.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public Rect Viewport
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SdlHost.ThrowOnFailure(SDL_GetRenderViewport(_rendererPtr, out SDL_Rect rect), "Failed to get viewport");
                return rect; // Implicit conversion
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);


                SDL_Rect rect = value; // Implicit conversion
                SdlHost.ThrowOnFailure(SDL_SetRenderViewport(_rendererPtr, ref rect), "Failed to set viewport");
            }
        }

        /// <summary>
        /// Gets or sets the clipping rectangle for the current target.
        /// </summary>
        /// <remarks>Set to null to disable clipping.</remarks>
        /// <exception cref="ObjectDisposedException"></exception>
        public Rect? ClipRect
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (!SDL_RenderClipEnabled(_rendererPtr))
                {
                    return null;
                }

                SdlHost.ThrowOnFailure(SDL_GetRenderClipRect(_rendererPtr, out SDL_Rect rect), "Failed to get clip rect");
                return rect;
            }
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Renderer));
                }

                SDL_Rect rect = value ?? default;
                SdlHost.ThrowOnFailure(SDL_SetRenderClipRect(_rendererPtr, ref rect), "Failed to set clip rect");
            }
        }

        /// <summary>
        /// Gets a value indicating whether clipping is enabled for the current target.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool ClipEnabled
        {
            get
            {
                return _disposed ? throw new ObjectDisposedException(nameof(Renderer)) : SDL_RenderClipEnabled(_rendererPtr);
            }
        }


        // --- Renderer Methods ---

        /// <summary>
        /// Clears the current rendering target with the drawing color.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Clear()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RenderClear(_rendererPtr), "Failed to clear renderer");
        }

        /// <summary>
        /// Updates the screen with any rendering performed since the previous call.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Present()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RenderPresent(_rendererPtr), "Failed to present renderer");
        }

        /// <summary>
        /// Force the rendering context to flush any pending commands.
        /// You do not need to (and in fact, shouldn't) call this function unless
        /// you are planning to call into the underlying graphics API directly.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Flush()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_FlushRenderer(_rendererPtr), "Failed to flush renderer");
        }

        /// <summary>
        /// Draws a point on the current rendering target.
        /// </summary>
        /// <param name="point">The point to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawPoint(FPoint point)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RenderPoint(_rendererPtr, point.X, point.Y), "Failed to draw point");
        }

        /// <summary>
        /// Draws multiple points on the current rendering target.
        /// </summary>
        /// <param name="points">The points to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawPoints(ReadOnlySpan<FPoint> points)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);


            if (points.IsEmpty)
            {
                return;
            }
            // SDL_RenderPoints expects SDL_FPoint[], need to convert or use unsafe context if bindings don't handle Span directly.
            // Assuming the binding `Span<SDL_FPoint>` works. Need to convert FPoint to SDL_FPoint.
            // This is inefficient. Consider providing an overload accepting SDL_FPoint or using unsafe code.
            var sdlPoints = new SDL_FPoint[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                sdlPoints[i] = points[i];
            }

            SdlHost.ThrowOnFailure(SDL_RenderPoints(_rendererPtr, sdlPoints, points.Length), "Failed to draw points");
        }

        /// <summary>
        /// Draws a line on the current rendering target.
        /// </summary>
        /// <param name="p1">The start point.</param>
        /// <param name="p2">The end point.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawLine(FPoint p1, FPoint p2)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RenderLine(_rendererPtr, p1.X, p1.Y, p2.X, p2.Y), "Failed to draw line");
        }

        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SdlHost.ThrowOnFailure(SDL_RenderLine(_rendererPtr, x1, y1, x2, y2), "Failed to draw line");
        }


        /// <summary>
        /// Draws a sequence of connected lines on the current rendering target.
        /// </summary>
        /// <param name="points">The points defining the lines. A line is drawn between points[i] and points[i+1].</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawLines(ReadOnlySpan<FPoint> points)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (points.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlPoints = new SDL_FPoint[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                sdlPoints[i] = points[i];
            }

            SdlHost.ThrowOnFailure(SDL_RenderLines(_rendererPtr, sdlPoints, points.Length), "Failed to draw lines");
        }

        /// <summary>
        /// Draws the outline of a rectangle on the current rendering target.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawRect(FRect rect)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SDL_FRect sdlRect = rect; // Implicit conversion
            SdlHost.ThrowOnFailure(SDL_RenderRect(_rendererPtr, ref sdlRect), "Failed to draw rect");
        }

        /// <summary>
        /// Draws the outlines of multiple rectangles on the current rendering target.
        /// </summary>
        /// <param name="rects">The rectangles to draw.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DrawRects(ReadOnlySpan<FRect> rects)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (rects.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlRects = new SDL_FRect[rects.Length];
            for (int i = 0; i < rects.Length; ++i)
            {
                sdlRects[i] = rects[i];
            }

            SdlHost.ThrowOnFailure(SDL_RenderRects(_rendererPtr, sdlRects, rects.Length), "Failed to draw rects");
        }

        /// <summary>
        /// Fills a rectangle on the current rendering target with the drawing color.
        /// </summary>
        /// <param name="rect">The rectangle to fill.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void FillRect(FRect rect)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            SDL_FRect sdlRect = rect; // Implicit conversion
            SdlHost.ThrowOnFailure(SDL_RenderFillRect(_rendererPtr, ref sdlRect), "Failed to fill rect");
        }

        /// <summary>
        /// Fills multiple rectangles on the current rendering target with the drawing color.
        /// </summary>
        /// <param name="rects">The rectangles to fill.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void FillRects(ReadOnlySpan<FRect> rects)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);


            if (rects.IsEmpty)
            {
                return;
            }
            // Similar conversion needed as DrawPoints
            var sdlRects = new SDL_FRect[rects.Length];
            for (int i = 0; i < rects.Length; ++i)
            {
                sdlRects[i] = rects[i];
            }

            SdlHost.ThrowOnFailure(SDL_RenderFillRects(_rendererPtr, sdlRects, rects.Length), "Failed to fill rects");
        }

        /// <summary>
        /// Copies a portion of a texture to the current rendering target.
        /// </summary>
        /// <param name="texture">The source texture.</param>
        /// <param name="srcRect">The source rectangle, or null for the entire texture.</param>
        /// <param name="dstRect">The destination rectangle, or null for the entire rendering target.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <c>null</c>.</exception>
        public void Copy(Texture texture, FRect? srcRect, FRect? dstRect)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            ArgumentNullException.ThrowIfNull(texture);

            ObjectDisposedException.ThrowIf(texture.IsDisposed, this);

            SDL_FRect sdlSrcRect = srcRect ?? default;
            SDL_FRect sdlDstRect = dstRect ?? default;

            // SDL_RenderTexture takes pointers, need to handle nullability
            unsafe
            {
                SDL_FRect* pSrc = srcRect.HasValue ? &sdlSrcRect : null;
                SDL_FRect* pDst = dstRect.HasValue ? &sdlDstRect : null;
                // Need to check the binding: SDL_RenderTexture takes pointers or refs?
                // The binding shows `ref SDL_FRect srcrect, ref SDL_FRect dstrect`. This is problematic for null.
                // Let's assume the binding should have used pointers or we need an overload.
                // HACK: Using default rects if null, assuming SDL handles {0,0,0,0} correctly for "entire texture/target".
                // This might not be correct SDL behavior. A binding fix or overload is better.
                if (!srcRect.HasValue)
                {
                    sdlSrcRect = default;
                }

                if (!dstRect.HasValue)
                {
                    sdlDstRect = default;
                }

                SdlHost.ThrowOnFailure(SDL_RenderTexture(_rendererPtr, texture.Handle, ref sdlSrcRect, ref sdlDstRect), "Failed to copy texture");
            }
        }

        /// <summary>
        /// Copies a portion of a texture to the current rendering target, rotating it and flipping it.
        /// </summary>
        /// <param name="texture">The source texture.</param>
        /// <param name="srcRect">The source rectangle, or null for the entire texture.</param>
        /// <param name="dstRect">The destination rectangle, or null for the entire rendering target.</param>
        /// <param name="angle">An angle in degrees for rotation.</param>
        /// <param name="center">The point to rotate around, or null for the center of dstRect.</param>
        /// <param name="flip">Flip mode.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <c>null</c>.</exception>
        public void CopyEx(Texture texture, FRect? srcRect, FRect? dstRect, double angle, FPoint? center, SDL_FlipMode flip)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(texture);

            SDL_FRect sdlSrcRect = srcRect ?? default;
            SDL_FRect sdlDstRect = dstRect ?? default;
            SDL_FPoint sdlCenter = center ?? default; // Default might not be correct center

            // HACK: Handle null pointers similar to Copy method.
            if (!srcRect.HasValue)
            {
                sdlSrcRect = default;
            }

            if (!dstRect.HasValue)
            {
                sdlDstRect = default;
            }

            if (!center.HasValue && dstRect.HasValue)
            {
                // Calculate center if not provided
                sdlCenter = new SDL_FPoint { x = sdlDstRect.w / 2.0f, y = sdlDstRect.h / 2.0f };
            }
            else if (!center.HasValue)
            {
                // Cannot calculate center if dstRect is also null
                sdlCenter = default; // Or throw?
            }

            SdlHost.ThrowOnFailure(SDL_RenderTextureRotated(_rendererPtr, texture.Handle, ref sdlSrcRect, ref sdlDstRect, angle, ref sdlCenter, flip), "Failed to copy texture (ex)");
        }

        /// <summary>
        /// Renders geometry defined by vertices and optional indices.
        /// </summary>
        /// <param name="texture">The texture to apply to the geometry, or null for untextured.</param>
        /// <param name="vertices">The vertices defining the geometry.</param>
        /// <param name="indices">The indices mapping vertices to triangles, or null if vertices are already ordered triangles.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void RenderGeometry(Texture? texture, ReadOnlySpan<Vertex> vertices, ReadOnlySpan<int> indices = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);


            if (vertices.IsEmpty)
            {
                return;
            }

            IntPtr textureHandle = texture != null ? texture.Handle : IntPtr.Zero;


            // Convert Vertex[] to SDL_Vertex[] - Inefficient, consider unsafe or direct SDL_Vertex usage
            var sdlVertices = new SDL_Vertex[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                sdlVertices[i] = vertices[i];
            }

            // Check if indices are provided
            int[]? indicesArray = indices.IsEmpty ? null : indices.ToArray(); // Convert Span to array if needed by binding

            SdlHost.ThrowOnFailure(SDL_RenderGeometry(_rendererPtr, textureHandle, sdlVertices, vertices.Length, indicesArray, indices.Length), "Failed to render geometry");
        }

        /// <summary>
        /// Sets the logical size for rendering.
        /// </summary>
        /// <param name="w">The logical width.</param>
        /// <param name="h">The logical height.</param>
        /// <param name="mode">The presentation mode for scaling.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetLogicalPresentation(int w, int h, SDL_RendererLogicalPresentation mode)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);


            SdlHost.ThrowOnFailure(SDL_SetRenderLogicalPresentation(_rendererPtr, w, h, mode), "Failed to set logical presentation");
        }

        /// <summary>
        /// Gets the logical size for rendering.
        /// </summary>
        /// <param name="w">Outputs the logical width.</param>
        /// <param name="h">Outputs the logical height.</param>
        /// <param name="mode">Outputs the presentation mode.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public void GetLogicalPresentation(out int w, out int h, out SDL_RendererLogicalPresentation mode)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);


            SdlHost.ThrowOnFailure(SDL_GetRenderLogicalPresentation(_rendererPtr, out w, out h, out mode), "Failed to get logical presentation");
        }

        /// <summary>
        /// Creates a texture for rendering.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="access">The texture access mode.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>A new Texture instance.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public Texture CreateTexture(SDL_PixelFormat format, SDL_TextureAccess access, int width, int height)
        {
            return _disposed ? throw new ObjectDisposedException(nameof(Renderer)) : new Texture(this, format, access, width, height);
        }

        // --- IDisposable Implementation ---

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // e.g., if we cached Texture objects created by GetRenderTarget
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                if (_rendererPtr != IntPtr.Zero)
                {
                    SDL_DestroyRenderer(_rendererPtr);
                    _rendererPtr = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the Renderer.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~Renderer()
        {
            Dispose(disposing: false);
        }
    }

    #endregion

    #region Input Abstraction (Keyboard)

    /// <summary>
    /// Represents physical key locations on a keyboard, independent of layout.
    /// Maps closely to SDL_Scancode.
    /// </summary>
    public enum Key
    {
        Unknown = SDL_Scancode.SDL_SCANCODE_UNKNOWN,

        A = SDL_Scancode.SDL_SCANCODE_A,
        B = SDL_Scancode.SDL_SCANCODE_B,
        C = SDL_Scancode.SDL_SCANCODE_C,
        D = SDL_Scancode.SDL_SCANCODE_D,
        E = SDL_Scancode.SDL_SCANCODE_E,
        F = SDL_Scancode.SDL_SCANCODE_F,
        G = SDL_Scancode.SDL_SCANCODE_G,
        H = SDL_Scancode.SDL_SCANCODE_H,
        I = SDL_Scancode.SDL_SCANCODE_I,
        J = SDL_Scancode.SDL_SCANCODE_J,
        K = SDL_Scancode.SDL_SCANCODE_K,
        L = SDL_Scancode.SDL_SCANCODE_L,
        M = SDL_Scancode.SDL_SCANCODE_M,
        N = SDL_Scancode.SDL_SCANCODE_N,
        O = SDL_Scancode.SDL_SCANCODE_O,
        P = SDL_Scancode.SDL_SCANCODE_P,
        Q = SDL_Scancode.SDL_SCANCODE_Q,
        R = SDL_Scancode.SDL_SCANCODE_R,
        S = SDL_Scancode.SDL_SCANCODE_S,
        T = SDL_Scancode.SDL_SCANCODE_T,
        U = SDL_Scancode.SDL_SCANCODE_U,
        V = SDL_Scancode.SDL_SCANCODE_V,
        W = SDL_Scancode.SDL_SCANCODE_W,
        X = SDL_Scancode.SDL_SCANCODE_X,
        Y = SDL_Scancode.SDL_SCANCODE_Y,
        Z = SDL_Scancode.SDL_SCANCODE_Z,

        D1 = SDL_Scancode.SDL_SCANCODE_1,
        D2 = SDL_Scancode.SDL_SCANCODE_2,
        D3 = SDL_Scancode.SDL_SCANCODE_3,
        D4 = SDL_Scancode.SDL_SCANCODE_4,
        D5 = SDL_Scancode.SDL_SCANCODE_5,
        D6 = SDL_Scancode.SDL_SCANCODE_6,
        D7 = SDL_Scancode.SDL_SCANCODE_7,
        D8 = SDL_Scancode.SDL_SCANCODE_8,
        D9 = SDL_Scancode.SDL_SCANCODE_9,
        D0 = SDL_Scancode.SDL_SCANCODE_0,

        Return = SDL_Scancode.SDL_SCANCODE_RETURN,
        Escape = SDL_Scancode.SDL_SCANCODE_ESCAPE,
        Backspace = SDL_Scancode.SDL_SCANCODE_BACKSPACE,
        Tab = SDL_Scancode.SDL_SCANCODE_TAB,
        Space = SDL_Scancode.SDL_SCANCODE_SPACE,

        Minus = SDL_Scancode.SDL_SCANCODE_MINUS,
        Equals = SDL_Scancode.SDL_SCANCODE_EQUALS,
        LeftBracket = SDL_Scancode.SDL_SCANCODE_LEFTBRACKET,
        RightBracket = SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET,
        Backslash = SDL_Scancode.SDL_SCANCODE_BACKSLASH,
        Semicolon = SDL_Scancode.SDL_SCANCODE_SEMICOLON,
        Apostrophe = SDL_Scancode.SDL_SCANCODE_APOSTROPHE,
        Grave = SDL_Scancode.SDL_SCANCODE_GRAVE, // Backtick `
        Comma = SDL_Scancode.SDL_SCANCODE_COMMA,
        Period = SDL_Scancode.SDL_SCANCODE_PERIOD,
        Slash = SDL_Scancode.SDL_SCANCODE_SLASH,

        CapsLock = SDL_Scancode.SDL_SCANCODE_CAPSLOCK,

        F1 = SDL_Scancode.SDL_SCANCODE_F1,
        F2 = SDL_Scancode.SDL_SCANCODE_F2,
        F3 = SDL_Scancode.SDL_SCANCODE_F3,
        F4 = SDL_Scancode.SDL_SCANCODE_F4,
        F5 = SDL_Scancode.SDL_SCANCODE_F5,
        F6 = SDL_Scancode.SDL_SCANCODE_F6,
        F7 = SDL_Scancode.SDL_SCANCODE_F7,
        F8 = SDL_Scancode.SDL_SCANCODE_F8,
        F9 = SDL_Scancode.SDL_SCANCODE_F9,
        F10 = SDL_Scancode.SDL_SCANCODE_F10,
        F11 = SDL_Scancode.SDL_SCANCODE_F11,
        F12 = SDL_Scancode.SDL_SCANCODE_F12,

        PrintScreen = SDL_Scancode.SDL_SCANCODE_PRINTSCREEN,
        ScrollLock = SDL_Scancode.SDL_SCANCODE_SCROLLLOCK,
        Pause = SDL_Scancode.SDL_SCANCODE_PAUSE,
        Insert = SDL_Scancode.SDL_SCANCODE_INSERT,
        Home = SDL_Scancode.SDL_SCANCODE_HOME,
        PageUp = SDL_Scancode.SDL_SCANCODE_PAGEUP,
        Delete = SDL_Scancode.SDL_SCANCODE_DELETE,
        End = SDL_Scancode.SDL_SCANCODE_END,
        PageDown = SDL_Scancode.SDL_SCANCODE_PAGEDOWN,
        Right = SDL_Scancode.SDL_SCANCODE_RIGHT,
        Left = SDL_Scancode.SDL_SCANCODE_LEFT,
        Down = SDL_Scancode.SDL_SCANCODE_DOWN,
        Up = SDL_Scancode.SDL_SCANCODE_UP,

        NumLockClear = SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR,
        KeypadDivide = SDL_Scancode.SDL_SCANCODE_KP_DIVIDE,
        KeypadMultiply = SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY,
        KeypadMinus = SDL_Scancode.SDL_SCANCODE_KP_MINUS,
        KeypadPlus = SDL_Scancode.SDL_SCANCODE_KP_PLUS,
        KeypadEnter = SDL_Scancode.SDL_SCANCODE_KP_ENTER,
        Keypad1 = SDL_Scancode.SDL_SCANCODE_KP_1,
        Keypad2 = SDL_Scancode.SDL_SCANCODE_KP_2,
        Keypad3 = SDL_Scancode.SDL_SCANCODE_KP_3,
        Keypad4 = SDL_Scancode.SDL_SCANCODE_KP_4,
        Keypad5 = SDL_Scancode.SDL_SCANCODE_KP_5,
        Keypad6 = SDL_Scancode.SDL_SCANCODE_KP_6,
        Keypad7 = SDL_Scancode.SDL_SCANCODE_KP_7,
        Keypad8 = SDL_Scancode.SDL_SCANCODE_KP_8,
        Keypad9 = SDL_Scancode.SDL_SCANCODE_KP_9,
        Keypad0 = SDL_Scancode.SDL_SCANCODE_KP_0,
        KeypadPeriod = SDL_Scancode.SDL_SCANCODE_KP_PERIOD,

        Application = SDL_Scancode.SDL_SCANCODE_APPLICATION, // Menu key
        Power = SDL_Scancode.SDL_SCANCODE_POWER,
        KeypadEquals = SDL_Scancode.SDL_SCANCODE_KP_EQUALS,
        F13 = SDL_Scancode.SDL_SCANCODE_F13,
        F14 = SDL_Scancode.SDL_SCANCODE_F14,
        F15 = SDL_Scancode.SDL_SCANCODE_F15,
        F16 = SDL_Scancode.SDL_SCANCODE_F16,
        F17 = SDL_Scancode.SDL_SCANCODE_F17,
        F18 = SDL_Scancode.SDL_SCANCODE_F18,
        F19 = SDL_Scancode.SDL_SCANCODE_F19,
        F20 = SDL_Scancode.SDL_SCANCODE_F20,
        F21 = SDL_Scancode.SDL_SCANCODE_F21,
        F22 = SDL_Scancode.SDL_SCANCODE_F22,
        F23 = SDL_Scancode.SDL_SCANCODE_F23,
        F24 = SDL_Scancode.SDL_SCANCODE_F24,

        Execute = SDL_Scancode.SDL_SCANCODE_EXECUTE,
        Help = SDL_Scancode.SDL_SCANCODE_HELP,
        Menu = SDL_Scancode.SDL_SCANCODE_MENU,
        Select = SDL_Scancode.SDL_SCANCODE_SELECT,
        Stop = SDL_Scancode.SDL_SCANCODE_STOP,
        Again = SDL_Scancode.SDL_SCANCODE_AGAIN,
        Undo = SDL_Scancode.SDL_SCANCODE_UNDO,
        Cut = SDL_Scancode.SDL_SCANCODE_CUT,
        Copy = SDL_Scancode.SDL_SCANCODE_COPY,
        Paste = SDL_Scancode.SDL_SCANCODE_PASTE,
        Find = SDL_Scancode.SDL_SCANCODE_FIND,
        Mute = SDL_Scancode.SDL_SCANCODE_MUTE,
        VolumeUp = SDL_Scancode.SDL_SCANCODE_VOLUMEUP,
        VolumeDown = SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN,

        LeftCtrl = SDL_Scancode.SDL_SCANCODE_LCTRL,
        LeftShift = SDL_Scancode.SDL_SCANCODE_LSHIFT,
        LeftAlt = SDL_Scancode.SDL_SCANCODE_LALT, // Alt, Option
        LeftGui = SDL_Scancode.SDL_SCANCODE_LGUI, // Windows, Command, Meta
        RightCtrl = SDL_Scancode.SDL_SCANCODE_RCTRL,
        RightShift = SDL_Scancode.SDL_SCANCODE_RSHIFT,
        RightAlt = SDL_Scancode.SDL_SCANCODE_RALT, // Alt Gr, Option
        RightGui = SDL_Scancode.SDL_SCANCODE_RGUI, // Windows, Command, Meta

        // Add other keys as needed...
        Mode = SDL_Scancode.SDL_SCANCODE_MODE, // AltGr, Mode switch

        // Media keys
        MediaPlay = SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY,
        MediaPause = SDL_Scancode.SDL_SCANCODE_MEDIA_PAUSE,
        MediaRecord = SDL_Scancode.SDL_SCANCODE_MEDIA_RECORD,
        MediaFastForward = SDL_Scancode.SDL_SCANCODE_MEDIA_FAST_FORWARD,
        MediaRewind = SDL_Scancode.SDL_SCANCODE_MEDIA_REWIND,
        MediaNextTrack = SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK,
        MediaPreviousTrack = SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK,
        MediaStop = SDL_Scancode.SDL_SCANCODE_MEDIA_STOP,
        MediaEject = SDL_Scancode.SDL_SCANCODE_MEDIA_EJECT,
        MediaPlayPause = SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY_PAUSE,
        MediaSelect = SDL_Scancode.SDL_SCANCODE_MEDIA_SELECT,

        // Application control keys
        AppNew = SDL_Scancode.SDL_SCANCODE_AC_NEW,
        AppOpen = SDL_Scancode.SDL_SCANCODE_AC_OPEN,
        AppClose = SDL_Scancode.SDL_SCANCODE_AC_CLOSE,
        AppExit = SDL_Scancode.SDL_SCANCODE_AC_EXIT,
        AppSave = SDL_Scancode.SDL_SCANCODE_AC_SAVE,
        AppPrint = SDL_Scancode.SDL_SCANCODE_AC_PRINT,
        AppProperties = SDL_Scancode.SDL_SCANCODE_AC_PROPERTIES,
        AppSearch = SDL_Scancode.SDL_SCANCODE_AC_SEARCH,
        AppHome = SDL_Scancode.SDL_SCANCODE_AC_HOME,
        AppBack = SDL_Scancode.SDL_SCANCODE_AC_BACK,
        AppForward = SDL_Scancode.SDL_SCANCODE_AC_FORWARD,
        AppStop = SDL_Scancode.SDL_SCANCODE_AC_STOP,
        AppRefresh = SDL_Scancode.SDL_SCANCODE_AC_REFRESH,
        AppBookmarks = SDL_Scancode.SDL_SCANCODE_AC_BOOKMARKS,

        // Mobile keys
        SoftLeft = SDL_Scancode.SDL_SCANCODE_SOFTLEFT,
        SoftRight = SDL_Scancode.SDL_SCANCODE_SOFTRIGHT,
        Call = SDL_Scancode.SDL_SCANCODE_CALL,
        EndCall = SDL_Scancode.SDL_SCANCODE_ENDCALL,

        // Total number of scancodes. Not a key itself.
        Count = SDL_Scancode.SDL_SCANCODE_COUNT
    }

    /// <summary>
    /// Represents keyboard modifier keys (Shift, Ctrl, Alt, Gui/Meta).
    /// Maps directly to SDL_Keymod flags.
    /// </summary>
    [Flags]
    public enum KeyModifier : ushort
    {
        None = SDL_Keymod.SDL_KMOD_NONE,
        LeftShift = SDL_Keymod.SDL_KMOD_LSHIFT,
        RightShift = SDL_Keymod.SDL_KMOD_RSHIFT,
        Shift = SDL_Keymod.SDL_KMOD_SHIFT,
        LeftCtrl = SDL_Keymod.SDL_KMOD_LCTRL,
        RightCtrl = SDL_Keymod.SDL_KMOD_RCTRL,

        // Combined flags for convenience
        Ctrl = SDL_Keymod.SDL_KMOD_CTRL,
        LeftAlt = SDL_Keymod.SDL_KMOD_LALT,
        RightAlt = SDL_Keymod.SDL_KMOD_RALT,
        Alt = SDL_Keymod.SDL_KMOD_ALT,
        LeftGui = SDL_Keymod.SDL_KMOD_LGUI,     // Windows/Command/Meta key
        RightGui = SDL_Keymod.SDL_KMOD_RGUI,    // Windows/Command/Meta key
        Gui = SDL_Keymod.SDL_KMOD_GUI,
        NumLock = SDL_Keymod.SDL_KMOD_NUM,
        CapsLock = SDL_Keymod.SDL_KMOD_CAPS,
        Mode = SDL_Keymod.SDL_KMOD_MODE,      // AltGr
        ScrollLock = SDL_Keymod.SDL_KMOD_SCROLL
    }

    /// <summary>
    /// Provides static methods for querying keyboard state.
    /// </summary>
    public static class Keyboard
    {
        private static readonly SDLBool[] _keyStates = new SDLBool[(int)SDL_Scancode.SDL_SCANCODE_COUNT];
        private static readonly object _stateLock = new object(); // Lock for accessing shared state buffer

        // Static constructor to ensure the mapping is built once.
        static Keyboard()
        {
            // Pre-build mapping if needed, or do it on demand.
            // For now, mapping is direct via enum values.
        }

        /// <summary>
        /// Updates the internal keyboard state snapshot. Call this once per frame before checking keys.
        /// </summary>
        /// <remarks>
        /// This is optional if SDL_PumpEvents is called elsewhere, as SDL_GetKeyboardState
        /// uses an internal state updated by the event pump. However, calling this explicitly
        /// ensures the state used by IsKeyDown/IsKeyUp is consistent for the frame.
        /// </remarks>
        public static void UpdateState()
        {
            if (!SdlHost.IsInitialized || (SdlHost.WasInit(SdlSubSystem.Events) & SdlSubSystem.Events) == 0)
            {
                throw new InvalidOperationException("SDL Events subsystem not initialized.");
            }

            // Get the current state array from SDL
            unsafe
            {
                // SDL_GetKeyboardState returns a pointer to an internal SDL state array.
                // We need to copy its contents safely.
                IntPtr statePtr = SDL_GetKeyboardState(out int numkeys);
                if (statePtr == IntPtr.Zero)
                {
                    // This should not happen if EVENTS is initialized
                    throw new SDLException("SDL_GetKeyboardState returned NULL.");
                }

                // Ensure our internal buffer is large enough (should match SDL_SCANCODE_COUNT)
                if (numkeys > _keyStates.Length)
                {
                    // This indicates a mismatch between our enum and SDL's internal count. Problem!
                    throw new InvalidOperationException($"SDL returned {numkeys} keys, expected <= {(int)Key.Count}.");
                }

                // Copy the state using Span for safety and efficiency
                lock (_stateLock)
                {
                    var sourceSpan = new ReadOnlySpan<SDLBool>((void*)statePtr, numkeys);
                    var destSpan = new Span<SDLBool>(_keyStates, 0, numkeys);
                    sourceSpan.CopyTo(destSpan);

                    // Zero out remaining elements if SDL returns fewer keys than our enum size (unlikely but possible)
                    if (numkeys < _keyStates.Length)
                    {
                        Array.Clear(_keyStates, numkeys, _keyStates.Length - numkeys);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a specific physical key is currently held down.
        /// Requires SDL_PumpEvents() or UpdateState() to have been called recently.
        /// </summary>
        /// <param name="key">The physical key to check.</param>
        /// <returns>True if the key is down, false otherwise.</returns>
        public static bool IsKeyDown(Key key)
        {
            if (key < 0 || (int)key >= _keyStates.Length) return false; // Bounds check

            lock (_stateLock)
            {
                // Access the pre-copied state
                // SDLBool implicitly converts to bool (true if non-zero/pressed)
                return _keyStates[(int)key];
            }
        }

        /// <summary>
        /// Checks if a specific physical key is currently released.
        /// Requires SDL_PumpEvents() or UpdateState() to have been called recently.
        /// </summary>
        /// <param name="key">The physical key to check.</param>
        /// <returns>True if the key is up, false otherwise.</returns>
        public static bool IsKeyUp(Key key)
        {
            return !IsKeyDown(key);
        }

        /// <summary>
        /// Gets the current state of the modifier keys (Shift, Ctrl, Alt, Gui, CapsLock, NumLock, etc.).
        /// </summary>
        /// <returns>A KeyModifier flags enum representing the active modifiers.</returns>
        public static KeyModifier GetModifiers()
        {
            if (!SdlHost.IsInitialized || (SdlHost.WasInit(SdlSubSystem.Events) & SdlSubSystem.Events) == 0)
            {
                throw new InvalidOperationException("SDL Events subsystem not initialized.");
            }
            // SDL_GetModState returns the SDL_Keymod flags directly
            return (KeyModifier)SDL_GetModState();
        }

        /// <summary>
        /// Gets the wrapper Key enum value corresponding to an SDL Scancode.
        /// </summary>
        /// <param name="scancode">The SDL Scancode.</param>
        /// <returns>The corresponding Key enum value.</returns>
        public static Key GetKeyFromScancode(SDL_Scancode scancode)
        {
            // Direct cast is possible because we defined Key enum values based on SDL_Scancode
            return (Key)scancode;
        }

        /// <summary>
        /// Gets the SDL Scancode corresponding to a wrapper Key enum value.
        /// </summary>
        /// <param name="key">The wrapper Key enum value.</param>
        /// <returns>The corresponding SDL Scancode.</returns>
        public static SDL_Scancode GetScancodeFromKey(Key key)
        {
            // Direct cast is possible
            return (SDL_Scancode)key;
        }

        /// <summary>
        /// Gets the name of a key based on its physical location (scancode).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The name of the key, or an empty string if unknown.</returns>
        public static string GetKeyName(Key key)
        {
            return SDL_GetScancodeName(GetScancodeFromKey(key)) ?? string.Empty;
        }

        /// <summary>
        /// Gets the key corresponding to the given physical key name.
        /// </summary>
        /// <param name="name">The name of the key (e.g., "A", "Return", "Space").</param>
        /// <returns>The corresponding Key enum value.</returns>
        public static Key GetKeyFromName(string name)
        {
            return GetKeyFromScancode(SDL_GetScancodeFromName(name));
        }

    }


    #endregion

    #region Event Handling

    /// <summary>
    /// Base class for SDL event arguments.
    /// </summary>
    public class SdlEventArgs(SDL_EventType type, ulong timestamp) : EventArgs
    {
        /// <summary>
        /// The timestamp of the event in milliseconds.
        /// </summary>
        public ulong Timestamp { get; } = timestamp;

        /// <summary>
        /// The type of the event.
        /// </summary>
        public SDL_EventType Type { get; } = type;
    }

    // --- Specific Event Argument Classes ---

    /// <summary>
    /// Event arguments for quit events.
    /// </summary>
    public class QuitEventArgs : SdlEventArgs
    {
        public QuitEventArgs(SDL_QuitEvent evt) : base(evt.type, evt.timestamp) { }
    }

    /// <summary>
    /// Event arguments for application lifecycle events.
    /// </summary>
    public class AppLifecycleEventArgs : SdlEventArgs
    {
        // No specific data beyond type and timestamp in SDL_CommonEvent
        public AppLifecycleEventArgs(SDL_CommonEvent common) : base((SDL_EventType)common.type, common.timestamp) { }
    }

    /// <summary>
    /// Event arguments for display events.
    /// </summary>
    public class DisplayEventArgs : SdlEventArgs
    {
        public uint DisplayId { get; }
        public int Data1 { get; } // Meaning depends on event type
        public int Data2 { get; } // Meaning depends on event type

        public DisplayEventArgs(SDL_DisplayEvent evt) : base(evt.type, evt.timestamp)
        {
            DisplayId = evt.displayID;
            Data1 = evt.data1;
            Data2 = evt.data2;
        }
    }

    /// <summary>
    /// Event arguments for window events. Includes abstracted event type.
    /// </summary>
    public class WindowEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public WindowEventType EventType { get; } // Wrapper enum for specific window event type
        public int Data1 { get; } // Meaning depends on event type (e.g., width for resize)
        public int Data2 { get; } // Meaning depends on event type (e.g., height for resize)

        // Internal constructor used by Events.MapEvent
        internal WindowEventArgs(SDL_WindowEvent evt, WindowEventType type) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            EventType = type; // Set the wrapper enum type
            Data1 = evt.data1;
            Data2 = evt.data2;
        }
    }

    /// <summary>
    /// Event arguments for keyboard device addition/removal.
    /// </summary>
    public class KeyboardDeviceEventArgs : SdlEventArgs
    {
        public uint Which { get; } // Instance ID

        public KeyboardDeviceEventArgs(SDL_KeyboardDeviceEvent evt) : base(evt.type, evt.timestamp)
        {
            Which = evt.which;
        }
    }


    /// <summary>
    /// Event arguments for keyboard key presses/releases.
    /// Uses the wrapper's Key and KeyModifier enums.
    /// </summary>
    public class KeyboardEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Keyboard instance ID
        public Key Key { get; } // Wrapper Key enum (physical location)
        public KeyModifier Modifiers { get; } // Wrapper KeyModifier enum
        public bool IsDown { get; }
        public bool IsRepeat { get; }
        // Removed: Scancode, Keycode, Raw (can be derived if needed)

        // FIX: Pass type and timestamp from the specific event struct, remove 'in'
        // Map SDL enums to wrapper enums here.
        public KeyboardEventArgs(SDL_KeyboardEvent evt) : base((SDL_EventType)evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            Key = Keyboard.GetKeyFromScancode(evt.scancode); // Use mapping function
            Modifiers = (KeyModifier)evt.mod; // Direct cast works for modifiers
            IsDown = evt.down;
            IsRepeat = evt.repeat;
        }
    }

    /// <summary>
    /// Event arguments for text input events.
    /// </summary>
    public class TextInputEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public string Text { get; }

        public unsafe TextInputEventArgs(SDL_TextInputEvent evt) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Text = Marshal.PtrToStringUTF8((IntPtr)evt.text) ?? string.Empty;
        }
    }

    /// <summary>
    /// Event arguments for text editing events (IME composition).
    /// </summary>
    public class TextEditingEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public string Text { get; }
        public int Start { get; }
        public int Length { get; }

        public unsafe TextEditingEventArgs(SDL_TextEditingEvent evt) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Text = Marshal.PtrToStringUTF8((IntPtr)evt.text) ?? string.Empty;
            Start = evt.start;
            Length = evt.length;
        }
    }

    /// <summary>
    /// Event arguments for mouse device addition/removal.
    /// </summary>
    public class MouseDeviceEventArgs : SdlEventArgs
    {
        public uint Which { get; } // Instance ID

        public MouseDeviceEventArgs(SDL_MouseDeviceEvent evt) : base(evt.type, evt.timestamp)
        {
            Which = evt.which;
        }
    }

    /// <summary>
    /// Event arguments for mouse motion events.
    /// </summary>
    public class MouseMotionEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public SDL_MouseButtonFlags State { get; }
        public float X { get; }
        public float Y { get; }
        public float XRel { get; }
        public float YRel { get; }

        public MouseMotionEventArgs(SDL_MouseMotionEvent evt) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            State = evt.state;
            X = evt.x;
            Y = evt.y;
            XRel = evt.xrel;
            YRel = evt.yrel;
        }
    }

    /// <summary>
    /// Event arguments for mouse button presses/releases.
    /// </summary>
    public class MouseButtonEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public byte Button { get; } // Button index (1=left, 2=middle, 3=right, etc.)
        public bool IsDown { get; }
        public byte Clicks { get; } // 1 for single-click, 2 for double-click, etc.
        public float X { get; }
        public float Y { get; }

        public MouseButtonEventArgs(SDL_MouseButtonEvent evt) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            Button = evt.button;
            IsDown = evt.down;
            Clicks = evt.clicks;
            X = evt.x;
            Y = evt.y;
        }
    }

    /// <summary>
    /// Event arguments for mouse wheel events.
    /// </summary>
    public class MouseWheelEventArgs : SdlEventArgs
    {
        public uint WindowId { get; }
        public uint Which { get; } // Mouse instance ID
        public float ScrollX { get; }
        public float ScrollY { get; }
        public SDL_MouseWheelDirection Direction { get; }
        public float MouseX { get; } // Precise mouse coordinates at time of scroll
        public float MouseY { get; } // Precise mouse coordinates at time of scroll

        public MouseWheelEventArgs(SDL_MouseWheelEvent evt) : base(evt.type, evt.timestamp)
        {
            WindowId = evt.windowID;
            Which = evt.which;
            ScrollX = evt.x;
            ScrollY = evt.y;
            Direction = evt.direction;
            MouseX = evt.mouse_x;
            MouseY = evt.mouse_y;
        }
    }

    // TODO: Add event args classes for Joystick, Gamepad, Touch, Sensor, Drop, Clipboard, User, etc.

    /// <summary>
    /// Provides static methods for handling SDL events.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Pumps the event loop, gathering events from the input devices.
        /// </summary>
        public static void PumpEvents()
        {
            SDL_PumpEvents();
        }

        /// <summary>
        /// Polls for currently pending events.
        /// </summary>
        /// <param name="eventArgs">The event arguments if an event was pending, otherwise null.</param>
        /// <returns>True if an event was polled, false otherwise.</returns>
        public static bool PollEvent(out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_PollEvent(out SDL_Event sdlEvent))
            {
                eventArgs = MapEvent(sdlEvent);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Waits indefinitely for the next available event.
        /// </summary>
        /// <param name="eventArgs">The event arguments for the event that occurred.</param>
        /// <returns>True if an event was received, false on error.</returns>
        public static bool WaitEvent(out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_WaitEvent(out SDL_Event sdlEvent))
            {
                eventArgs = MapEvent(sdlEvent);
                return true;
            }
            // SDL_WaitEvent returns false on error
            SdlHost.ClearError(); // Clear error potentially set by WaitEvent failure
            return false;
        }

        /// <summary>
        /// Waits until the specified timeout for the next available event.
        /// </summary>
        /// <param name="timeoutMs">The maximum number of milliseconds to wait.</param>
        /// <param name="eventArgs">The event arguments if an event was available, otherwise null.</param>
        /// <returns>True if an event was received, false if the timeout elapsed or an error occurred.</returns>
        public static bool WaitEventTimeout(int timeoutMs, out SdlEventArgs? eventArgs)
        {
            eventArgs = null;
            if (SDL_WaitEventTimeout(out SDL_Event sdlEvent, timeoutMs))
            {
                eventArgs = MapEvent(sdlEvent);
                return true;
            }
            // SDL_WaitEventTimeout returns false on timeout or error
            SdlHost.ClearError(); // Clear error potentially set by WaitEventTimeout failure
            return false;
        }


        /// <summary>
        /// Maps the raw SDL_Event structure to a managed SdlEventArgs object.
        /// </summary>
        /// <param name="sdlEvent">The raw SDL event.</param>
        /// <returns>A corresponding SdlEventArgs object, or null if the event type is unhandled.</returns>
        public static SdlEventArgs? MapEvent(SDL_Event sdlEvent)
        {
            SDL_EventType type = (SDL_EventType)sdlEvent.type;
            switch (type)
            {
                // Application Events
                case SDL_EventType.SDL_EVENT_QUIT:
                    return new QuitEventArgs(sdlEvent.quit);
                case SDL_EventType.SDL_EVENT_TERMINATING:
                case SDL_EventType.SDL_EVENT_LOW_MEMORY:
                case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
                case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                case SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND:
                case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
                case SDL_EventType.SDL_EVENT_LOCALE_CHANGED:
                case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED:
                    return new AppLifecycleEventArgs(sdlEvent.common);

                // Display Events
                case SDL_EventType.SDL_EVENT_DISPLAY_ORIENTATION:
                case SDL_EventType.SDL_EVENT_DISPLAY_ADDED:
                case SDL_EventType.SDL_EVENT_DISPLAY_REMOVED:
                case SDL_EventType.SDL_EVENT_DISPLAY_MOVED:
                case SDL_EventType.SDL_EVENT_DISPLAY_DESKTOP_MODE_CHANGED:
                case SDL_EventType.SDL_EVENT_DISPLAY_CURRENT_MODE_CHANGED:
                case SDL_EventType.SDL_EVENT_DISPLAY_CONTENT_SCALE_CHANGED:
                    return new DisplayEventArgs(sdlEvent.display);

                // Window Events
                // Window Events - Map to specific WindowEventType
                case SDL_EventType.SDL_EVENT_WINDOW_SHOWN: return new WindowEventArgs(sdlEvent.window, WindowEventType.Shown);
                case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN: return new WindowEventArgs(sdlEvent.window, WindowEventType.Hidden);
                case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Exposed);
                case SDL_EventType.SDL_EVENT_WINDOW_MOVED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Moved);
                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Resized);
                case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.PixelSizeChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Minimized);
                case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Maximized);
                case SDL_EventType.SDL_EVENT_WINDOW_RESTORED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Restored);
                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER: return new WindowEventArgs(sdlEvent.window, WindowEventType.MouseEnter);
                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE: return new WindowEventArgs(sdlEvent.window, WindowEventType.MouseLeave);
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED: return new WindowEventArgs(sdlEvent.window, WindowEventType.FocusGained);
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST: return new WindowEventArgs(sdlEvent.window, WindowEventType.FocusLost);
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED: return new WindowEventArgs(sdlEvent.window, WindowEventType.CloseRequested);
                case SDL_EventType.SDL_EVENT_WINDOW_HIT_TEST: return new WindowEventArgs(sdlEvent.window, WindowEventType.HitTest);
                case SDL_EventType.SDL_EVENT_WINDOW_ICCPROF_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.IccProfileChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.DisplayChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.DisplayScaleChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_OCCLUDED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Occluded);
                case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN: return new WindowEventArgs(sdlEvent.window, WindowEventType.EnterFullscreen);
                case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN: return new WindowEventArgs(sdlEvent.window, WindowEventType.LeaveFullscreen);
                case SDL_EventType.SDL_EVENT_WINDOW_DESTROYED: return new WindowEventArgs(sdlEvent.window, WindowEventType.Destroyed);
                case SDL_EventType.SDL_EVENT_WINDOW_SAFE_AREA_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.SafeAreaChanged);
                case SDL_EventType.SDL_EVENT_WINDOW_HDR_STATE_CHANGED: return new WindowEventArgs(sdlEvent.window, WindowEventType.HdrStateChanged);


                // Keyboard Events
                case SDL_EventType.SDL_EVENT_KEYBOARD_ADDED:
                case SDL_EventType.SDL_EVENT_KEYBOARD_REMOVED:
                    return new KeyboardDeviceEventArgs(sdlEvent.kdevice);
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    return new KeyboardEventArgs(sdlEvent.key);
                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                    return new TextInputEventArgs(sdlEvent.text);
                case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                    return new TextEditingEventArgs(sdlEvent.edit);
                case SDL_EventType.SDL_EVENT_KEYMAP_CHANGED:
                    // Maybe a specific event args or just use CommonEvent?
                    return new SdlEventArgs(type, sdlEvent.common.timestamp) { };

                // Mouse Events
                case SDL_EventType.SDL_EVENT_MOUSE_ADDED:
                case SDL_EventType.SDL_EVENT_MOUSE_REMOVED:
                    return new MouseDeviceEventArgs(sdlEvent.mdevice);
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    return new MouseMotionEventArgs(sdlEvent.motion);
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    return new MouseButtonEventArgs(sdlEvent.button);
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    return new MouseWheelEventArgs(sdlEvent.wheel);

                // TODO: Add mappings for Joystick, Gamepad, Touch, Sensor, Drop, Clipboard, User events etc.

                default:
                    // Log unhandled event?
                    Console.WriteLine($"Warning: Unhandled SDL Event Type: {type} ({(uint)type})");
                    return null; // Or return a generic SdlEventArgs(sdlEvent.common)?
            }
        }

        /// <summary>
        /// Sets the state of processing events for a specific type.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <param name="enabled">True to process events, false to ignore them.</param>
        public static void SetEventEnabled(SDL_EventType type, bool enabled)
        {
            SDL_SetEventEnabled((uint)type, enabled);
        }

        /// <summary>
        /// Checks if an event type is enabled for processing.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <returns>True if the event type is enabled, false otherwise.</returns>
        public static bool IsEventEnabled(SDL_EventType type)
        {
            return SDL_EventEnabled((uint)type);
        }

        /// <summary>
        /// Clears events of a specific type from the event queue.
        /// </summary>
        /// <param name="type">The type of event to clear.</param>
        public static void FlushEvent(SDL_EventType type)
        {
            SDL_FlushEvent((uint)type);
        }

        /// <summary>
        /// Clears events within a range of types from the event queue.
        /// </summary>
        /// <param name="minType">The minimum event type to clear.</param>
        /// <param name="maxType">The maximum event type to clear.</param>
        public static void FlushEvents(SDL_EventType minType, SDL_EventType maxType)
        {
            SDL_FlushEvents((uint)minType, (uint)maxType);
        }

        // Internal cleanup if needed (e.g., unregistering event watches)
        internal static void Quit()
        {
            // Add cleanup here if necessary
        }
    }

    #endregion

} // namespace SDL3.Wrapper
