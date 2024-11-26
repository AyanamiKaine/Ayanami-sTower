
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS.Events;
namespace Avalonia.Flecs.Example;

public partial class App : Application
{
    World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var window = _world.Entity("MainWindow")
            .Set(
                new Window()
                {
                    Title = "Avlonia.Flecs Example",
                    Height = 400,
                    Width = 400
                });

        var dockPanel = _world.Entity("MainWindowDockPanel")
            .ChildOf(window)
            .Set(new DockPanel() { });

        var menu = _world.Entity("MainMenu")
            .ChildOf(dockPanel)
            .Set(new Menu() { });

        _world.Entity("FileMenuItem")
            .ChildOf(menu)
            .Set(new MenuItem()
            {
                Header = "_File"
            });

        _world.Entity("EditMenuItem")
                    .ChildOf(menu)
                    .Set(new MenuItem()
                    {
                        Header = "_Edit"
                    });

        _world.Entity("SelectionMenuItem")
              .ChildOf(menu)
              .Set(new MenuItem()
              {
                  Header = "_Selection"
              });

        var textBlock = _world.Entity("HelloWorldTextBlock")
            .ChildOf(dockPanel)
            .Set(
                new TextBlock()
                {
                    Text = "Avalonia + Flecs = ♥"
                });

        var button = _world.Entity("Button")
            .ChildOf(dockPanel)
            .Set(
                new Button()
                {
                    Content = "CLICK ME"
                }
            );

        button.Observe<Click>((Entity e) =>
        {
            var clickEventData = e.Get<Click>();
            var b = clickEventData.Sender as Button;
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