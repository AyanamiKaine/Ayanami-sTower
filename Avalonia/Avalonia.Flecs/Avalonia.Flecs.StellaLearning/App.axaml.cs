using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.StellaLearning.Pages;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
namespace Avalonia.Flecs.StellaLearning;

public partial class App : Application
{

    private World _world = World.Create();
    private NamedEntities? _entities;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Controls.ECS.Module>();
        _world.Import<FluentUI.Controls.ECS.Module>();
        _world.Observer();
        _entities = new NamedEntities(_world);
        _world.Set<ScriptManager>(new(_world, _entities));
        _world.Get<ScriptManager>().CompileScriptsFromFolder("scripts/");

        var window = _world.Entity("MainWindow")
            .Set(new Window())
            .SetWindowTitle("Stella Learning")
            .SetHeight(400)
            .SetWidth(400);

        var navigationView = _world.Entity("NavigationView")
            .Set(new NavigationView())
            .SetPaneTitle("Stella Learning")
            .ChildOf(window)
            .SetColumn(0);


        var scrollViewer = _world.Entity("ScrollViewer")
            .ChildOf(navigationView)
            .Set(new ScrollViewer());

        var stackPanel = _world.Entity("StackPanel")
            .ChildOf(scrollViewer)
            .Set(new StackPanel());

        var grid = _world.Entity("MainContentDisplay")
            .ChildOf(stackPanel)
            .Set(new Grid())
            .SetColumnDefinitions(new ColumnDefinitions("2,*,*"))
            .SetRowDefinitions(new RowDefinitions("Auto"));

        var settingPage = SettingsPage.Create(_world, navigationView)
            .SetRow(2)
            .SetColumnSpan(3);

        var homePage = HomePage.Create(_world, "HomePage")
            .SetRow(2)
            .SetColumnSpan(3);

        var literaturePage = LiteraturePage.Create(_world)
            .SetRow(2)
            .SetColumnSpan(3);

        var spacedRepetitionPage = SpacedRepetitionPage.Create(_world);
        //spacedRepetitionPage.ChildOf(navigationView);

        _world.Entity("HomeNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Home");

        _world.Entity("LiteratureNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Literature");

        _world.Entity("SpacedRepetitionNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Spaced Repetition");


        navigationView.OnDisplayModeChanged((sender, args) =>
        {
            var e = navigationView;

            if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                e.Children((Entity child) =>
                {
                    if (child.Has<Controls.ECS.Module.Page>() && child.Has<Control>())
                    {
                        child.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                    }
                });
            }
            else if (args.DisplayMode == NavigationViewDisplayMode.Compact)
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


        navigationView.Observe<OnSelectionChanged>((Entity e) =>
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
        });

        navigationView.OnNavViewSelectionChanged(async (sender, args) =>
        {

            await Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                navigationView.Emit<OnSelectionChanged>();
            });
            if (sender is not NavigationView e)
                return;

            var selectedItem = args.SelectedItem as NavigationViewItem;
            if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Home")
            {
                //navigationView.Get<NavigationView>().Content = homePage.Get<TextBlock>();
                homePage.ChildOf(navigationView);

                //Maybe we could implement an event for the navigation view entity that says
                //something like new page added and than changes the margin of the page
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    homePage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    homePage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Literature")
            {
                //Console.WriteLine("Selection Changed To Literature");
                //navigationView.Get<NavigationView>().Content = literaturePage.Get<TextBlock>();
                literaturePage.ChildOf(navigationView);
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    literaturePage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    literaturePage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Spaced Repetition")
            {
                //navigationView.Get<NavigationView>().Content = spacedRepetitionPage.Get<Panel>();
                spacedRepetitionPage.ChildOf(navigationView);
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    spacedRepetitionPage.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    spacedRepetitionPage.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Settings")
            {
                //Console.WriteLine("Selection Changed To Settings");
                //navigationView.Get<NavigationView>().Content = settingPage.Get<TextBlock>();
                settingPage.ChildOf(navigationView);
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
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