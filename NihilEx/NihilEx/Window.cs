using SDL3;

namespace NihilEx;

public class Window(
    string title,
    int width,
    int height,
    bool isBorderless = false,
    bool isResizable = true,
    bool isFullscreen = false,
    bool isHidden = false,
    bool isModal = false,
    bool isHighPixelDensity = false,
    bool isAlwaysOnTop = false,
    bool isUtility = false,
    bool isTooltip = false,
    bool isPopupMenu = false,
    bool isTransparent = false,
    bool isNotFocusable = false) : IDisposable
{
    internal nint _window; // Store the SDL_Window pointer
    internal nint _renderer; // Store the SDL_Renderer pointer
    private bool _isDisposed = false;
    public bool IsFullscreen { get; } = isFullscreen;
    public bool IsHidden { get; } = isHidden;
    public bool IsBorderless { get; } = isBorderless;
    public bool IsResizable { get; } = isResizable;
    public bool IsModal { get; } = isModal;
    public bool IsHighPixelDensity { get; } = isHighPixelDensity;
    public bool IsAlwaysOnTop { get; } = isAlwaysOnTop;
    public bool IsUtility { get; } = isUtility;
    public bool IsTooltip { get; } = isTooltip;
    public bool IsPopupMenu { get; } = isPopupMenu;
    public bool IsTransparent { get; } = isTransparent;
    public bool IsNotFocusable { get; } = isNotFocusable;

    public int Width { get; } = width;
    public int Height { get; } = height;
    public string Title { get; } = title;

    public bool Initialize()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Events | SDL.InitFlags.Sensor))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            return false;
        }

        SDL.WindowFlags flags = 0; // Start with no flags

        if (IsFullscreen) flags |= SDL.WindowFlags.Fullscreen;
        if (IsHidden) flags |= SDL.WindowFlags.Hidden;
        if (IsBorderless) flags |= SDL.WindowFlags.Borderless;
        if (IsResizable) flags |= SDL.WindowFlags.Resizable;
        if (IsModal) flags |= SDL.WindowFlags.Modal;
        if (IsHighPixelDensity) flags |= SDL.WindowFlags.HighPixelDensity;
        if (IsAlwaysOnTop) flags |= SDL.WindowFlags.AlwaysOnTop;
        if (IsUtility) flags |= SDL.WindowFlags.Utility;
        if (IsTooltip) flags |= SDL.WindowFlags.Tooltip;
        if (IsPopupMenu) flags |= SDL.WindowFlags.PopupMenu;
        if (IsTransparent) flags |= SDL.WindowFlags.Transparent;
        if (IsNotFocusable) flags |= SDL.WindowFlags.NotFocusable;

        if (!SDL.CreateWindowAndRenderer(Title, Width, Height, flags, out _window, out _renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
            return false;
        }

        return true;
    }

    public void SetRenderDrawColor(byte r, byte g, byte b, byte a)
    {
        SDL.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void RenderClear()
    {
        SDL.RenderClear(_renderer);
    }

    public void RenderPresent()
    {
        SDL.RenderPresent(_renderer);
    }

    public bool PollEvent(out SDL.Event e)
    {
        return SDL.PollEvent(out e);
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed resources if needed (none for now)
            }

            // Free unmanaged resources (SDL resources)

            if (_renderer != 0) SDL.DestroyRenderer(_renderer);
            if (_window != 0) SDL.DestroyWindow(_window);

            SDL.Quit(); // Potentially move SDL.Quit() outside of Window class if other SDL functionalities will be used independently
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~Window()
    {
        Dispose(disposing: false);
    }
}