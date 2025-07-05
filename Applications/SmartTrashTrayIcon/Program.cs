using Avalonia;
using Avalonia.Flecs.Util;
using System;

namespace SmartTrashTrayIcon;

static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        /*
        Implementation done by Medo64
        (https://github.com/medo64/Medo/blob/main/examples/SingleInstanceApplication/App.cs)
        I simply copied it into my own repo for better maintainablility and updated it to
        support dotnet 9
        */
        SingleInstance.Attach(); // will auto-exit for second instance

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
