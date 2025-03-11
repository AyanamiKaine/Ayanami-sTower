using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using System;
using Avalonia.Flecs.Controls.ECS;

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
                new Window())
                .SetWindowTitle("Avalonia.Flecs Example")
                .SetHeight(400)
                .SetWidth(400);

        var navigationView = _world.Entity()
            .ChildOf(window)
            .Set(new NavigationView())
            .SetPaneTitle("Avalonia + Flecs = ❤️");

        var homePage = _world.Entity()
            .ChildOf(navigationView)
            .Set(new TextBlock())
            .SetText("Home");

        var settingPage = _world.Entity()
            .ChildOf(navigationView)
            .Set(new TextBlock())
            .SetText("Settings");

        navigationView.SetColumn(0);

        // We should probably put the event classes into an event class
        // so we can write Events.OnSelectionChanged instead of Module.OnSelectionChanged
        navigationView.OnNavViewSelectionChanged((sender, args) =>
        {
            if (args.SelectedItem is not NavigationViewItem selectedItem)
            {
                return;
            }

            if (selectedItem.Content is null)
            {
                return;
            }

            if (selectedItem?.Content.ToString() == "Home")
            {
                Console.WriteLine("Selection Changed To Home");
            }
            else if (selectedItem?.Content.ToString() == "Settings")
            {
                Console.WriteLine("Selection Changed To Settings");
            }
        });
        /*
        navigationView.Get<NavigationView>().SelectionChanged += (sender, args) =>
        {
            var selectedItem = args.SelectedItem as NavigationViewItem;

            if (selectedItem?.Content.ToString() == "Home")
            {
                navigationView.Get<NavigationView>().Content = homePage.Get<TextBlock>();
            }
            else if (selectedItem?.Content.ToString() == "Settings")
            {
                navigationView.Get<NavigationView>().Content = settingPage.Get<TextBlock>();

            }
        };
        */
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