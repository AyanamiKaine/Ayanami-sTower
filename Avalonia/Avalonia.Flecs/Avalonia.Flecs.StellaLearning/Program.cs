using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Flecs.Util;
using DesktopNotifications;
using DesktopNotifications.Avalonia;

namespace Avalonia.Flecs.StellaLearning;


/*
We implement stella learning as a single app, you cannot run more than one instance of the same app.
Because we want to run stella learning in the background. And instead of opening a new instance 
of the app we check if it already runs and show its window instead.
*/
static class Program
{
    public static INotificationManager NotificationManager = null!;
    public static bool StartHidden { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        /*
        Implementation done my Medo64
        (https://github.com/medo64/Medo/blob/main/examples/SingleInstanceApplication/App.cs)
        I simply copied it into my own repo for better maintainablility.
        */
        SingleInstance.Attach();  // will auto-exit for second instance
        SingleInstance.NewInstanceDetected += SingleInstance_NewInstanceDetected;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .SetupDesktopNotifications(out NotificationManager!)
                .WithInterFont()
                .LogToTrace();
    }

    private static void SingleInstance_NewInstanceDetected(object? sender, NewInstanceEventArgs e)
    {
        // Signal the UI thread to show/activate the main window
        Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (App.Current is App app)
            {
                app.ShowMainWindow();
            }
        });
    }
}
