using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Flecs.NET.Bindings;
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

        _world.RunRESTAPI();


        _entities = new NamedEntities(_world);
        _world.Set<ScriptManager>(new(_world, _entities));
        var scriptManager = _world.Get<ScriptManager>();

        scriptManager.OnScriptCompilationStart += (sender, args) => Console.WriteLine($"Start Compilation of: {args.ScriptName}");

        scriptManager.OnScriptCompilationFinished += (sender, args) =>
        {
            Console.WriteLine($"Finishes Compilation of: {args.ScriptName}");
            if (args.ScriptName == "main")
            {
                Console.WriteLine("Running Main script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    _entities.Clear();
                    await scriptManager.RunScriptAsync("main");
                    await scriptManager.StartReplAsync();
                });
            }
        };
        _world.Get<ScriptManager>().CompileScriptsFromFolder("scripts/");

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
                        Console.WriteLine("Deleting all named entities");
                        Console.WriteLine("Re-running Main script, This reloads the logic of the application");
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