using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Example;

public partial class App : Application
{
    World _world = World.Create();


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        var window = _world.Entity("MainWindow")
            .Set(
                new Window()
                {
                    Title = "Avlonia.Flecs Example",
                    Height = 400,
                    Width = 400
                });

        var dockPanel = _world.Entity("DockPanel")
            .ChildOf(window)
            .Set(new DockPanel());

        var navigationView = _world.Entity("NavigationView")
            .ChildOf(dockPanel)
            .Set(new NavigationView()
            {
                PaneTitle = "Avalonia + Flecs + FluentUI",
                IsBackButtonVisible = true,
                IsBackEnabled = true,
            });

        var navigationViewItem = _world.Entity("NavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Home"
            });
        

        var frame = _world.Entity("Frame")
            .ChildOf(dockPanel)
            .Set(new Frame());

        var textBlock = _world.Entity("TextBlock")
            .ChildOf(dockPanel)
            .Set(new TextBlock()
            {
                Text = "Hello, World!"
            });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _world.Lookup("MainWindow").Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}