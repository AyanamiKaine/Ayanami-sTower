using System;
using System.Drawing;
using System.Threading; // For Thread.Sleep


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
        Console.WriteLine(value: "Starting SDL3 Wrapper Example...");

        try
        {
            // 1. Initialize SDL Video and Events subsystems
            // SdlHost handles initialization and throws on failure.
            SdlHost.Init(flags: SdlSubSystem.Video);
            Console.WriteLine(value: "SDL Initialized successfully.");

            // 2. Create a Window
            // The 'using' statement ensures window.Dispose() is called automatically
            // when exiting the block, which in turn calls SDL_DestroyWindow.
            using (var window = new Window(title: "My SDL3 Wrapper Window",width: 800, height: 600,flags: WindowFlags.Resizable))
            {
                Console.WriteLine(value: $"Window created with ID: {window.Id}");

                // 2b. Create a Renderer for the Window
                // Place renderer creation within the window's using block.
                // The renderer's using statement ensures it's disposed before the window.
                using (var renderer = window.CreateRenderer())
                {
                    Console.WriteLine(value: $"Renderer created: {renderer.Name}");
                    renderer.VSync = true;

                    // 3. Main Loop
                    bool running = true;
                    Console.WriteLine(value: "Entering main loop...");
                    while (running)
                    {
                        //Events.PumpEvents();
                        // 4. Process Events
                        // Poll for events and process them using the wrapper's event args.
                        Keyboard.UpdateState();
                        while (Events.PollEvent(eventArgs: out SdlEventArgs? evt))
                        {
                            if (evt == null) break; // Should generally not happen if PollEvent returns true

                            // Handle specific event types
                            switch (evt)
                            {
                                case QuitEventArgs:
                                    Console.WriteLine(value: "Quit event received.");
                                    running = false;
                                    break;

                                case WindowEventArgs windowEvt:
                                    // Use the wrapper WindowEventType enum
                                    Console.WriteLine(value: $"Window Event: {windowEvt.EventType} on Window {windowEvt.WindowId}");
                                    if (windowEvt.EventType == WindowEventType.CloseRequested &&
                                        windowEvt.WindowId == window.Id)
                                    {
                                        Console.WriteLine(value: "Window close requested.");
                                        running = false;
                                    }
                                    // Handle other window events like Resized, Moved etc. using windowEvt.EventType
                                    if (windowEvt.EventType == WindowEventType.Resized)
                                    {
                                        Console.WriteLine(value: $"Window resized to {windowEvt.Data1}x{windowEvt.Data2}");
                                    }
                                    break;

                                case KeyboardEventArgs keyEvt:
                                    // Use the new wrapper enums
                                    if (keyEvt.IsDown) // Check if it's a key press event
                                    {
                                        Console.WriteLine(value: $"Key Down Event: Key={keyEvt.Key}, Modifiers={keyEvt.Modifiers}, Repeat={keyEvt.IsRepeat}");

                                        // Example: Quit on Escape key press (using event args)
                                        if (keyEvt.Key == Key.Escape)
                                        {
                                            Console.WriteLine(value: "Escape key pressed (event).");
                                            running = false;
                                        }

                                        // Example: Check for Ctrl+C combo (using event args)
                                        if (keyEvt.Key == Key.C && keyEvt.Modifiers.HasFlag(flag: KeyModifier.Ctrl))
                                        {
                                            Console.WriteLine(value: "Ctrl+C pressed (event).");
                                            // Note: This only triggers when C is pressed *while* Ctrl is held.
                                        }
                                    }
                                    else // Key Up event
                                    {
                                        Console.WriteLine(value: $"Key Up Event: Key={keyEvt.Key}, Modifiers={keyEvt.Modifiers}");
                                    }
                                    break;

                                case MouseButtonEventArgs mouseBtnEvt:
                                    Console.WriteLine(value: $"Mouse Button: {mouseBtnEvt.Button} Down: {mouseBtnEvt.IsDown} Clicks: {mouseBtnEvt.Clicks} at ({mouseBtnEvt.X}, {mouseBtnEvt.Y})");
                                    break;

                                // Add more event cases as needed (e.g., MouseMotionEventArgs)

                                default:
                                    Console.WriteLine(value: $"Unhandled Event Type: {evt.Type}");
                                    break;
                            }
                        } // End event polling loop

                        if (Keyboard.IsKeyDown(key: Key.W) && Keyboard.IsKeyDown(key: Key.LeftShift))
                        {
                            //Console.WriteLine("W and Left Shift are currently held down (polling).");
                        }

                        // Example: Check current modifiers
                        KeyModifier currentMods = Keyboard.GetModifiers();
                        if (currentMods.HasFlag(flag: KeyModifier.LeftAlt))
                        {
                            //Console.WriteLine("Alt is currently held down.");
                        }

                        // 5. Add Application Logic Here (Update)
                        // (e.g., Update game state)


                        // 6. Render graphics
                        // Set draw color (e.g., Cornflower Blue)
                        renderer.DrawColor = new Color(r: 100, g: 149, b:237); // Using wrapper Color struct
                                                                       // Clear the back buffer
                        renderer.Clear();

                        // --- Add drawing code here ---
                        // Example: Draw a white rectangle
                        renderer.DrawColor = Color.White;
                        renderer.FillRect(rect: new FRect(x: 100,y: 100, w: 200,h: 150));

                        renderer.DrawColor = Color.Red;
                        renderer.DrawLine(p1: new FPoint(x: 0,y: 20), p2: new FPoint(x:200,y: 500));

                        // -----------------------------

                        // Present the back buffer to the screen
                        renderer.Present();
                    } // End main loop

                    Console.WriteLine(value: "Exiting main loop...");

                } // Renderer Dispose() called here

                Console.WriteLine(value: "Renderer disposed.");

            } // Window Dispose() called here

            Console.WriteLine(value: "Window disposed.");

        }
        catch (SDLException sdlEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(value: $"SDL Exception: {sdlEx.Message}");
            Console.ResetColor();
            Environment.ExitCode = 1; // Indicate failure
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(value: $"Unhandled Exception: {ex}");
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
                Console.WriteLine(value: "SDL Quit successfully.");
            }
        }
        Console.WriteLine(value: "Application finished.");
    }
}
