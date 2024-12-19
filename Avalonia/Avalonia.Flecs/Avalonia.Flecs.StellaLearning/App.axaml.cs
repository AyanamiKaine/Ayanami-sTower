using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.StellaLearning.Pages;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
using System;
using Avalonia.Threading;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using FSRSPythonBridge;
using System.Threading.Tasks;
using System.IO;
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

        _entities = new NamedEntities(_world);

        // we define our own scripting options because we 
        // need to add references to FluentAvalonia that is by 
        // default not loaded by the scripting manager
        var scriptingOptions = ScriptOptions.Default
            .WithSourceResolver(new ScriptSourceRefrenceResolver())
            .AddReferences(
                typeof(object).Assembly, // Usually needed for basic types
                Assembly.Load("Flecs.NET"),
                Assembly.Load("Flecs.NET.Bindings"),
                Assembly.Load("Avalonia.Flecs.Controls"),
                Assembly.Load("Avalonia.Controls"),
                Assembly.Load("Avalonia.Flecs.Scripting"),
                Assembly.Load("Avalonia"),
                Assembly.Load("Avalonia.Desktop"),
                Assembly.Load("Avalonia.Base"),
                Assembly.Load("FluentAvalonia")
            // Load your API assembly
            )
            .AddImports("Avalonia.Controls")
            .AddImports("Avalonia.Flecs.Controls.ECS")
            .AddImports("Avalonia.Flecs.Scripting")
            .AddImports("Flecs.NET.Core");


        _world.Set<ScriptManager>(new(_world, _entities, scriptingOptions));
        var scriptManager = _world.Get<ScriptManager>();

        scriptManager.OnScriptCompilationStart += (sender, args) => Console.WriteLine($"Start Compilation of: {args.ScriptName}");

        scriptManager.OnScriptCompilationFinished += (sender, args) =>
        {

            // Here we write all the scripts we would like to 
            // automatically recompile and run if changed 

            Console.WriteLine($"Finishes Compilation of: {args.ScriptName}");
            if (args.ScriptName == "main")
            {
                Console.WriteLine("Running Main script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    _entities.Clear();
                    await scriptManager.RunScriptAsync("main");
                });
            }

            if (args.ScriptName == "KnowledgeVaultPage")
            {
                Console.WriteLine("Running KnowledgeVaultPage script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await scriptManager.RunScriptAsync("KnowledgeVaultPage");
                });
            }

            if (args.ScriptName == "ContentQueuePage")
            {
                Console.WriteLine("Running ContentQueuePage script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await scriptManager.RunScriptAsync("ContentQueuePage");
                });
            }

            if (args.ScriptName == "LiteraturePage")
            {
                Console.WriteLine("Running LiteraturePage script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await scriptManager.RunScriptAsync("LiteraturePage");
                });
            }

            if (args.ScriptName == "SpacedRepetitionPage")
            {
                Console.WriteLine("Running SpacedRepetitionPage script");
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await scriptManager.RunScriptAsync("SpacedRepetitionPage");
                });
            }
        };

        _world.Get<ScriptManager>().CompileScriptsFromFolder("scripts/");


        var window = _entities.Create("MainWindow")
            .Set(new Window())
            .SetWindowTitle("Stella Learning")
            .SetHeight(400)
            .SetWidth(400);

        var navigationView = _entities.Create("NavigationView")
            .Set(new NavigationView())
            .SetPaneTitle("Stella Learning")
            .ChildOf(window)
            .SetColumn(0);

        var scrollViewer = _entities.Create("ScrollViewer")
            .ChildOf(navigationView)
            .Set(new ScrollViewer());

        var stackPanel = _entities.Create("StackPanel")
            .ChildOf(scrollViewer)
            .Set(new StackPanel());

        var grid = _entities.Create("MainContentDisplay")
            .ChildOf(stackPanel)
            .Set(new Grid())
            .SetColumnDefinitions("2,*,*")
            .SetRowDefinitions("Auto");

        var settingPage = SettingsPage.Create(_world, navigationView)
            .SetRow(2)
            .SetColumnSpan(3);

        var homePage = HomePage.Create(_world, "HomePage")
            .SetRow(2)
            .SetColumnSpan(3);

        var literaturePage = _entities.GetEntityCreateIfNotExist("LiteraturePage");

        var spacedRepetitionPage = _entities.GetEntityCreateIfNotExist("SpacedRepetitionPage");

        _entities.Create("HomeNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Home");

        _entities.Create("KnowledgeVaultNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Knowledge Vault");

        _entities.Create("ContentQueueNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Content Queue");

        _entities.Create("LiteratureNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Literature");

        /*
        The study page will be something more complex
        it will represent various topics and different 
        study methods specifically made for the topic.

        For example for the Painting topic we could have
        speed drawing in various times, refrence studies,
        color studies, master studies. 

        While for programming we could have code challenges,
        and adding specific themes and quizes for the topic.

        For example for garbage collection, generic programming,
        object oriented programming, functional programming, etc.
        */
        _entities.Create("StudyNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem())
            .SetProperty("Content", "Study");

        _entities.Create("SpacedRepetitionNavigationViewItem")
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

        navigationView.Observe<OnSelectionChanged>((Entity _) =>
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
            await Threading.Dispatcher.UIThread.InvokeAsync(() => navigationView.Emit<OnSelectionChanged>());

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
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Knowledge Vault")
            {
                _entities["KnowledgeVaultPage"].ChildOf(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    _entities["KnowledgeVaultPage"].Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    _entities["KnowledgeVaultPage"].Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Content Queue")
            {
                _entities["ContentQueuePage"].ChildOf(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    _entities["ContentQueuePage"].Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                else
                    _entities["ContentQueuePage"].Get<Control>().Margin = new Thickness(20, 10, 20, 20);
            }
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && _entities is not null)
        {
            desktop.MainWindow = _entities["MainWindow"].Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
#if DEBUG
        this.AttachDevTools();
#endif

    }
}