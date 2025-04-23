using System;
using System.Drawing;
using System.Threading; // For Thread.Sleep

// Include the wrapper namespace
using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;
using static SDL3.SDL;
// Include the SDL bindings namespace for enums like SDL_InitFlags

namespace AyanamisTower.NihilEx.SDLWrapper.MinimalExample;

/// <summary>
/// Minimal example using the wrapper
/// </summary>
public static class Program
{
    /// <summary>
    /// Entry point
    /// </summary>
    /// <param name="args"></param>
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting SDL3 Wrapper Example...");

        try
        {
            // 1. Initialize SDL Video and Events subsystems
            // SdlHost handles initialization and throws on failure.
            SdlHost.Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS);
            Console.WriteLine("SDL Initialized successfully.");

            // 2. Create a Window
            // The 'using' statement ensures window.Dispose() is called automatically
            // when exiting the block, which in turn calls SDL_DestroyWindow.
            using (var window = new Window("My SDL3 Wrapper Window", 800, 600, SDL_WindowFlags.SDL_WINDOW_RESIZABLE))
            {
                Console.WriteLine($"Window created with ID: {window.Id}");

                // 2b. Create a Renderer for the Window
                // Place renderer creation within the window's using block.
                // The renderer's using statement ensures it's disposed before the window.
                using (var renderer = window.CreateRenderer())
                {
                    Console.WriteLine($"Renderer created: {renderer.Name}");

                    // 3. Main Loop
                    bool running = true;
                    Console.WriteLine("Entering main loop...");
                    while (running)
                    {
                        Events.PumpEvents();
                        // 4. Process Events
                        // Poll for events and process them using the wrapper's event args.
                        while (Events.PollEvent(out SdlEventArgs? evt))
                        {
                            if (evt == null) break; // Should generally not happen if PollEvent returns true

                            // Handle specific event types
                            switch (evt)
                            {
                                case QuitEventArgs:
                                    Console.WriteLine("Quit event received.");
                                    running = false;
                                    break;

                                case WindowEventArgs windowEvt:
                                    // Check if the close button was clicked for *our* window
                                    if (windowEvt.Type == SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED &&
                                        windowEvt.WindowId == window.Id)
                                    {
                                        Console.WriteLine("Window close requested.");
                                        running = false;
                                    }
                                    Console.WriteLine($"Window Event: {windowEvt.Type} on Window {windowEvt.WindowId}");
                                    break;

                                case KeyboardEventArgs keyEvt:
                                    // Example: Quit on Escape key press
                                    if (keyEvt.IsDown && keyEvt.Keycode == SDL_Keycode.SDLK_ESCAPE)
                                    {
                                        Console.WriteLine("Escape key pressed.");
                                        running = false;
                                    }
                                    Console.WriteLine($"Key Event: {keyEvt.Keycode} ({keyEvt.Scancode}) Mod: {keyEvt.Modifiers} Down: {keyEvt.IsDown} Repeat: {keyEvt.IsRepeat}");
                                    break;

                                case MouseButtonEventArgs mouseBtnEvt:
                                    Console.WriteLine($"Mouse Button: {mouseBtnEvt.Button} Down: {mouseBtnEvt.IsDown} Clicks: {mouseBtnEvt.Clicks} at ({mouseBtnEvt.X}, {mouseBtnEvt.Y})");
                                    break;

                                // Add more event cases as needed (e.g., MouseMotionEventArgs)

                                default:
                                    Console.WriteLine($"Unhandled Event Type: {evt.Type}");
                                    break;
                            }
                        } // End event polling loop

                        // 5. Add Application Logic Here (Update)
                        // (e.g., Update game state)


                        // 6. Render graphics
                        // Set draw color (e.g., Cornflower Blue)
                        renderer.DrawColor = new Color(100, 149, 237); // Using wrapper Color struct
                                                                       // Clear the back buffer
                        renderer.Clear();

                        // --- Add drawing code here ---
                        // Example: Draw a white rectangle
                        // renderer.DrawColor = Color.White;
                        // renderer.FillRect(new FRect(100, 100, 200, 150));
                        // -----------------------------

                        // Present the back buffer to the screen
                        renderer.Present();
                    } // End main loop

                    Console.WriteLine("Exiting main loop...");

                } // Renderer Dispose() called here

                Console.WriteLine("Renderer disposed.");

            } // Window Dispose() called here

            Console.WriteLine("Window disposed.");

        }
        catch (SDLException sdlEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"SDL Exception: {sdlEx.Message}");
            Console.ResetColor();
            Environment.ExitCode = 1; // Indicate failure
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unhandled Exception: {ex}");
            Console.ResetColor();
            Environment.ExitCode = 1; // Indicate failure
        }
        finally
        {
            // 7. Quit SDL
            // SdlHost.Quit() cleans up all initialized subsystems.
            if (SdlHost.IsInitialized)
            {
                SdlHost.Quit();
                Console.WriteLine("SDL Quit successfully.");
            }
        }
        Console.WriteLine("Application finished.");
    }
}
