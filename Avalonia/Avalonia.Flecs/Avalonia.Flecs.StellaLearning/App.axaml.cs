using Flecs.NET.Core;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.FluentUI.Controls.ECS.Module;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Flecs.StellaLearning.Pages;
using Avalonia.Markup.Xaml.MarkupExtensions;
namespace Avalonia.Flecs.StellaLearning;

public partial class App : Application
{

    private World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();
        _world.Observer();

        var window = _world.Entity("MainWindow")
            .Set(
                new Window()
                {
                    Title = "Stella Learning",
                    Height = 400,
                    Width = 400
                });



        var navigationView = _world.Entity("NavigationView")
                    .ChildOf(window)
                    .Set(new NavigationView()
                    {
                        PaneTitle = "Stella Learning",
                    });


        var scrollViewer = _world.Entity("ScrollViewer")
            .ChildOf(navigationView)
            .Set(new ScrollViewer());

        var stackPanel = _world.Entity("StackPanel")
            .ChildOf(scrollViewer)
            .Set(new StackPanel());

        var grid = _world.Entity("MainContentDisplay")
            .ChildOf(stackPanel)
            .Set(new Grid()
            {
                ColumnDefinitions = new ColumnDefinitions("2,*,*"),
                RowDefinitions = new RowDefinitions("Auto")
            });

        var settingPage = SettingsPage.Create(_world, navigationView);

        Grid.SetRow(settingPage.Get<Control>(), 2);
        Grid.SetColumnSpan(settingPage.Get<Control>(), 3);

        var homePage = HomePage.Create(_world, "HomePage");
        Grid.SetRow(homePage.Get<Control>(), 2);
        Grid.SetColumnSpan(homePage.Get<Control>(), 3);

        var literaturePage = LiteraturePage.Create(_world);

        Grid.SetRow(literaturePage.Get<Control>(), 2);
        Grid.SetColumnSpan(literaturePage.Get<Control>(), 3);


        var spacedRepetitionPage = SpacedRepetitionPage.Create(_world);
        //spacedRepetitionPage.ChildOf(navigationView);

        _world.Entity("HomeNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Home"
            });

        _world.Entity("LiteratureNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Literature"
            });

        _world.Entity("SpacedRepetitionNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Spaced Repetition"
            });

        Grid.SetColumn(navigationView.Get<NavigationView>(), 0);

        navigationView.Observe<FluentUI.Controls.ECS.Events.OnDisplayModeChanged>((Entity e) =>
        {

            var OnDisplayModeChanged = e.Get<FluentUI.Controls.ECS.Events.OnDisplayModeChanged>();

            if (OnDisplayModeChanged.Args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                e.Children((Entity child) =>
                {
                    if (child.Has<Controls.ECS.Module.Page>() && child.Has<Control>())
                    {
                        child.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                    }
                });
            }
            else if (OnDisplayModeChanged.Args.DisplayMode == NavigationViewDisplayMode.Compact)
            {
                e.Children((Entity child) =>
                {
                    if (child.Has<Controls.ECS.Module.Page>() && child.Has<Control>())
                    {
                        child.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
                    }
                });

            }

        });


        navigationView.Observe<FluentUI.Controls.ECS.Events.OnSelectionChanged>((Entity e) =>
        {

            // We first remove any other page ensuring 
            // that only the selected page is displayed
            navigationView.Children((Entity child) =>
            {
                if (child.Has<Controls.ECS.Module.Page>())
                {
                    child.Remove(Ecs.ChildOf, Ecs.Wildcard);
                }
            });

            var OnSelectionChanged = e.Get<FluentUI.Controls.ECS.Events.OnSelectionChanged>();

            var selectedItem = OnSelectionChanged.Args.SelectedItem as NavigationViewItem;
            if (selectedItem?.Content.ToString() == "Home")
            {
                //navigationView.Get<NavigationView>().Content = homePage.Get<TextBlock>();
                homePage.ChildOf(navigationView);

                //Maybe we could implement an event for the navigation view entity that says
                //something like new page added and than changes the margin of the page
                if (e.Get<NavigationView>().DisplayMode == NavigationViewDisplayMode.Minimal)
                    homePage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    homePage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content.ToString() == "Literature")
            {
                //Console.WriteLine("Selection Changed To Literature");
                //navigationView.Get<NavigationView>().Content = literaturePage.Get<TextBlock>();
                literaturePage.ChildOf(navigationView);
                if (e.Get<NavigationView>().DisplayMode == NavigationViewDisplayMode.Minimal)
                    literaturePage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    literaturePage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content.ToString() == "Spaced Repetition")
            {
                //navigationView.Get<NavigationView>().Content = spacedRepetitionPage.Get<Panel>();
                spacedRepetitionPage.ChildOf(navigationView);
                if (e.Get<NavigationView>().DisplayMode == NavigationViewDisplayMode.Minimal)
                    spacedRepetitionPage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    spacedRepetitionPage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content.ToString() == "Settings")
            {
                //Console.WriteLine("Selection Changed To Settings");
                //navigationView.Get<NavigationView>().Content = settingPage.Get<TextBlock>();
                settingPage.ChildOf(navigationView);
                if (e.Get<NavigationView>().DisplayMode == NavigationViewDisplayMode.Minimal)
                    settingPage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    settingPage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
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