
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Flecs.Controls.ECS;
namespace Avalonia.Flecs.Example;

public partial class App : Application
{
    World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var window = _world.Entity("MainWindow")
            .Set(new Window())
            .SetWindowTitle("Avlonia.Flecs Example")
            .SetHeight(400)
            .SetWidth(400);

        var dockPanel = _world.Entity("MainWindowDockPanel")
            .ChildOf(window)
            .Set(new DockPanel());

        var menu = _world.Entity("MainMenu")
            .ChildOf(dockPanel)
            .Set(new Menu());

        _world.Entity("FileMenuItem")
            .ChildOf(menu)
            .Set(new MenuItem())
            .SetHeader("_File");

        _world.Entity("EditMenuItem")
                    .ChildOf(menu)
                    .Set(new MenuItem())
                    .SetHeader("_Edit");

        _world.Entity("SelectionMenuItem")
              .ChildOf(menu)
              .Set(new MenuItem())
              .SetHeader("_Selection");

        var textBlock = _world.Entity("HelloWorldTextBlock")
            .ChildOf(dockPanel)
            .Set(new TextBlock())
            .SetText("Hello World!");

        var button = _world.Entity("Button")
            .ChildOf(dockPanel)
            .Set(new Button())
            .SetContent("Click Me!");

        button.OnClick((sender, args) =>
        {
            if (sender is not Button b)
            {
                return;
            }

            b.Content = "CHANGED CONTENT";
            Console.WriteLine("THE BUTTON WAS CLICK WHOAH!");
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