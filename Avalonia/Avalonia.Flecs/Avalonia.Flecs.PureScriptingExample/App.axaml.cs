using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Flecs.NET.Core;

namespace Avalonia.Flecs.PureScriptingExample;




public partial class App : Application
{
    private World _world = World.Create();
    private NamedEntities? _entities;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Controls.ECS.Module>();
        _world.Import<FluentUI.Controls.ECS.Module>();

        _entities = new NamedEntities(_world);
        _world.Set<ScriptManager>(new(_world, _entities));
        _world.Get<ScriptManager>().CompileScriptsFromFolder("scripts/");
        var scriptManager = _world.Get<ScriptManager>();

        var window = _world.Entity("MainWindow")
            .Set(new Window())
            .SetWindowTitle("Stella Learning")
            .SetHeight(400)
            .SetWidth(400)
            .OnKeyDown((sender, args) =>
            {
                if (args.Key == Key.F5)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        Console.WriteLine("Re-running Main script");
                        _entities.Clear();
                        await scriptManager.RunScriptAsync("main");
                    });

                    // Optionally mark the event as handled
                    args.Handled = true;
                }
            });

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _world.Lookup("MainWindow").Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}