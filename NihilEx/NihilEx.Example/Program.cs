using SDL3;

namespace NihilEx.Example;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var window = new Window("SDL3 Abstraction Window", 800, 600); // Create Window object and ensure disposal
        if (!window.Initialize()) // Initialize SDL and create window/renderer
        {
            return; // Initialization failed, exit
        }

        window.SetRenderDrawColor(100, 149, 237, 0);

        var loop = true;

        while (loop)
        {
            while (window.PollEvent(out var e))
            {
                if (e.Type == (uint)SDL.EventType.Quit)
                {
                    loop = false;
                }
            }

            window.RenderClear();
            window.RenderPresent();
        }
    }
}