using System;

namespace AyanamisTower.StellaEcs.Engine;

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

/// <summary>
/// Represents a window in the SDL application.
/// </summary>
public sealed unsafe class Window : IDisposable
{
    private bool flash;
    private ObjectHandle<Window> ObjectHandle { get; }
    private SDL_Window* sdlWindowHandle;
    private SDL_Renderer* renderer;
    private readonly bool initSuccess;

    private const SDL_InitFlags init_flags = SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD;

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    public Window()
    {
        if (!SDL_InitSubSystem(init_flags))
            throw new InvalidOperationException($"failed to initialise SDL. Error: {SDL_GetError()}");

        initSuccess = true;

        ObjectHandle = new ObjectHandle<Window>(this, GCHandleType.Normal);
    }

    /// <summary>
    /// Sets up the window for use.
    /// </summary>
    public void Setup()
    {
        SDL_SetGamepadEventsEnabled(true);
        SDL_SetEventFilter(&NativeFilter, ObjectHandle.Handle);

        if (OperatingSystem.IsWindows())
            SDL_SetWindowsMessageHook(&WndProc, ObjectHandle.Handle);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static SDLBool WndProc(IntPtr userdata, MSG* message)
    {
        var handle = new ObjectHandle<Window>(userdata);

        if (handle.GetTarget(out var window))
        {
            Console.WriteLine($"from {window}, message: {message->message}");
        }

        return true;
    }

    // ReSharper disable once UseCollectionExpression
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static SDLBool NativeFilter(IntPtr userdata, SDL_Event* e)
    {
        var handle = new ObjectHandle<Window>(userdata);
        if (handle.GetTarget(out var window))
            return window.HandleEventFromFilter(e);

        return true;
    }

    /// <summary>
    /// An action to be invoked for each event that passes the filter.
    /// </summary>
    public Action<SDL_Event>? EventFilter;

    private bool HandleEventFromFilter(SDL_Event* e)
    {
        switch (e->Type)
        {
            case SDL_EventType.SDL_EVENT_KEY_UP:
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                HandleKeyFromFilter(e->key);
                break;

            default:
                EventFilter?.Invoke(*e);
                break;
        }

        return true;
    }

    private void HandleKeyFromFilter(SDL_KeyboardEvent e)
    {
        if (e.key == SDL_Keycode.SDLK_F)
        {
            flash = true;
        }
    }

    /// <summary>
    /// Creates the SDL window and renderer.
    /// </summary>
    public void Create()
    {
        sdlWindowHandle = SDL_CreateWindow("hello"u8, 800, 600, SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY);
        renderer = SDL_CreateRenderer(sdlWindowHandle, (Utf8String)null);
    }

    /// <summary>
    /// Handles SDL events.
    /// </summary>
    /// <param name="e"></param>
    private void HandleEvent(SDL_Event e)
    {
        switch (e.Type)
        {
            case SDL_EventType.SDL_EVENT_QUIT:
                run = false;
                break;

            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                switch (e.key.key)
                {
                    case SDL_Keycode.SDLK_R:
                        bool old = SDL_GetWindowRelativeMouseMode(sdlWindowHandle);
                        SDL_SetWindowRelativeMouseMode(sdlWindowHandle, !old);
                        break;

                    case SDL_Keycode.SDLK_V:
                        string? text = SDL_GetClipboardText();
                        Console.WriteLine($"clipboard: {text}");
                        break;

                    case SDL_Keycode.SDLK_F10:
                        SDL_SetWindowFullscreen(sdlWindowHandle, false);
                        break;

                    case SDL_Keycode.SDLK_F11:
                        SDL_SetWindowFullscreen(sdlWindowHandle, true);
                        break;

                    case SDL_Keycode.SDLK_J:
                        {
                            using var gamepads = SDL_GetGamepads();

                            if (gamepads == null || gamepads.Count == 0)
                                break;

                            var gamepad = SDL_OpenGamepad(gamepads[0]);

                            int count;
                            var bindings = SDL_GetGamepadBindings(gamepad, &count);

                            for (int i = 0; i < count; i++)
                            {
                                var binding = *bindings[i];
                                Console.WriteLine(binding.input_type);
                                Console.WriteLine(binding.output_type);
                                Console.WriteLine();
                            }

                            SDL_CloseGamepad(gamepad);
                            break;
                        }

                    case SDL_Keycode.SDLK_F1:
                        SDL_StartTextInput(sdlWindowHandle);
                        break;

                    case SDL_Keycode.SDLK_F2:
                        SDL_StopTextInput(sdlWindowHandle);
                        break;

                    case SDL_Keycode.SDLK_M:
                        SDL_Keymod mod = e.key.mod;
                        Console.WriteLine(mod);
                        break;

                    case SDL_Keycode.SDLK_E:
                        Console.WriteLine(SDL_GetEventDescription(e));
                        break;
                }

                break;

            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                Console.WriteLine(e.text.GetText());
                break;

            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                Console.WriteLine($"gamepad added: {e.gdevice.which}");
                break;

            case SDL_EventType.SDL_EVENT_PEN_PROXIMITY_IN:
                Console.WriteLine($"pen proximity in: {e.pproximity.which}");
                break;
        }
    }

    private bool run = true;

    private const int events_per_peep = 64;
    private readonly SDL_Event[] events = new SDL_Event[events_per_peep];

    private void PollEvents()
    {
        SDL_PumpEvents();

        int eventsRead;

        do
        {
            eventsRead = SDL_PeepEvents(events, SDL_EventAction.SDL_GETEVENT, SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
            for (int i = 0; i < eventsRead; i++)
                HandleEvent(events[i]);
        } while (eventsRead == events_per_peep);
    }

    private float frame;

    /// <summary>
    /// Runs the main application loop.
    /// </summary>
    public void Run()
    {
        while (run)
        {
            if (flash)
            {
                flash = false;
                Console.WriteLine("flash!");
            }

            PollEvents();

            SDL_SetRenderDrawColorFloat(renderer, (SDL_sinf(frame) / 2) + 0.5f, (SDL_cosf(frame) / 2) + 0.5f, 0.3f, 1.0f);
            SDL_RenderClear(renderer);
            SDL_RenderPresent(renderer);

            frame += 0.015f;

            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Disposes the window and renderer.
    /// </summary>
    public void Dispose()
    {
        if (initSuccess)
            SDL_QuitSubSystem(init_flags);

        ObjectHandle.Dispose();
    }
}

