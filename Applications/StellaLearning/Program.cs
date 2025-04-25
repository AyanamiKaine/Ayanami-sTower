/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using Avalonia;
using Avalonia.Flecs.Util;
using DesktopNotifications;
using DesktopNotifications.Avalonia;

namespace AyanamisTower.StellaLearning;

/*
We implement stella learning as a single app, you cannot run more than one instance of the same app.
Because we want to run stella learning in the background. And instead of opening a new instance
of the app we check if it already runs and show its window instead.
*/
static class Program
{
    public static INotificationManager NotificationManager = null!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        DotNetEnv.Env.TraversePath().Load();

        /*
        Implementation done by Medo64
        (https://github.com/medo64/Medo/blob/main/examples/SingleInstanceApplication/App.cs)
        I simply copied it into my own repo for better maintainablility and updated it to
        support dotnet 9
        */
        SingleInstance.Attach(); // will auto-exit for second instance
        SingleInstance.NewInstanceDetected += OnNewInstanceDetected;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .SetupDesktopNotifications(out NotificationManager!)
            .WithInterFont()
            .LogToTrace();
    }

    private static void OnNewInstanceDetected(object? sender, NewInstanceEventArgs e)
    {
        // Signal the UI thread to show/activate the main window
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (App.Current is App app)
            {
                app.ShowMainWindow();
            }
        });
    }
}
