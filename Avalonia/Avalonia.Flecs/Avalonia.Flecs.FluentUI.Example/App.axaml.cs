using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using System;

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


        var navigationView = _world.Entity("NavigationView")
            .ChildOf(window)
            .Set(new NavigationView()
            {
                PaneTitle = "Avalonia + Flecs + FluentUI",
            });

        var homePage = _world.Entity("HomePage")
            .ChildOf(navigationView)
            .Set(new TextBlock()
            {
                Text = "This is Home!"
            });


        var settingPage = _world.Entity("SettingPage")
            .ChildOf(navigationView)
            .Set(new TextBlock()
            {
                Text = "Settings"
            });

        var navigationViewItem = _world.Entity("NavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Home"
            });


        Grid.SetColumn(navigationView.Get<NavigationView>(), 0);

        // We should probably put the event classes into an event class
        // so we can write Events.OnSelectionChanged instead of Module.OnSelectionChanged
        navigationView.Observe<FluentUI.Controls.ECS.Events.OnSelectionChanged>((Entity e) =>
        {
            var OnSelectionChanged = e.Get<FluentUI.Controls.ECS.Events.OnSelectionChanged>();

            var selectedItem = OnSelectionChanged.Args.SelectedItem as NavigationViewItem;

            if (selectedItem is null)
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