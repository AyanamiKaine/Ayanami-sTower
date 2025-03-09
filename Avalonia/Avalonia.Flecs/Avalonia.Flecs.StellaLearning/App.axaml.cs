using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
using Avalonia.Flecs.Util;
using Avalonia.Flecs.StellaLearning.Pages;
using DesktopNotifications;
using System;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform;
using NLog;
using Avalonia.Flecs.Controls;

namespace Avalonia.Flecs.StellaLearning;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private World _world = World.Create();

    // TODO: Remove all refrences to name entities,
    // its a global that will cause many problems when grown too big.
    /// <summary>
    /// Named global app entities
    /// </summary>
    public static NamedEntities? Entities = null;
    private TrayIcon? _trayIcon;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Initializes the application.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        _world.Import<Controls.ECS.Module>();
        _world.Import<FluentUI.Controls.ECS.Module>();

        Entities = new NamedEntities(_world);

        //var debugWindow = new Debug.Window.Window(_world);
        //Entities.OnEntityAdded += debugWindow.AddEntity;

        var window = Entities.Create("MainWindow")
            .Set(new Window()
            {
                ShowActivated = false,
            })
            .SetWindowTitle("Stella Learning")
            .SetHeight(400)
            .SetWidth(400)
            .OnClosing((sender, args) =>
            {
                args.Cancel = true;

                if (sender is Window win)
                {
                    // We dont close the main window but instead hide it,
                    // because we have a tray icon that is still active.
                    win.Hide();
                }
            });
        CreateUILayout().ChildOf(window);
        InitializeTrayIcon();
    }

    private Entity CreateUILayout()
    {
        ContentQueuePage contentQueuePage = new(_world);
        KnowledgeVaultPage knowledgeVaultPage = new(_world);
        SettingsPage settingsPage = new(_world);
        HomePage homePage = new(_world);
        LiteraturePage literaturePage = new(_world);
        SpacedRepetitionPage spacedRepetitionPage = new(_world);

        var navigationView = _world.UI<NavigationView>(nav =>
        {
            nav.Property("PaneTitle", "Stella Learning")
               .Column(0);

            // Child elements are nested in the lambda, showing hierarchy
            nav.Child<ScrollViewer>(scroll =>
            {
                scroll.Child<StackPanel>(stack =>
                {
                    stack.Child<Grid>(grid =>
                    {
                        grid.Entity
                            .SetColumnDefinitions("2,*,*")
                            .SetRowDefinitions("Auto");
                    });
                });
            });

            nav.Child<NavigationViewItem>(item => item.Property("Content", "Home"));

            nav.Child<NavigationViewItem>(item => item.Property("Content", "Knowledge Vault"));

            nav.Child<NavigationViewItem>(item => item.Property("Content", "Literature"));

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

            nav.Child<NavigationViewItem>(item => item.Property("Content", "Study"));
            nav.Child<NavigationViewItem>(item => item.Property("Content", "Spaced Repetition"));

        });

        ((IUIComponent)spacedRepetitionPage).Attach(navigationView);

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
                ((IUIComponent)homePage).Attach(navigationView);

                //Maybe we could implement an event for the navigation view entity that says
                //something like new page added and than changes the margin of the page
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)homePage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)homePage).SetMargin(new Thickness(20, 10, 20, 20));
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Literature")
            {
                ((IUIComponent)literaturePage).Attach(navigationView);
                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)literaturePage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)literaturePage).SetMargin(new Thickness(20, 10, 20, 20));
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Spaced Repetition")
            {
                ((IUIComponent)spacedRepetitionPage).Attach(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)spacedRepetitionPage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)spacedRepetitionPage).SetMargin(new Thickness(20, 10, 20, 20));
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Settings")
            {
                ((IUIComponent)settingsPage).Attach(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)settingsPage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)settingsPage).SetMargin(new Thickness(20, 10, 20, 20));
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Knowledge Vault")
            {
                ((IUIComponent)knowledgeVaultPage).Attach(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)knowledgeVaultPage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)knowledgeVaultPage).SetMargin(new Thickness(20, 10, 20, 20));
            }
            else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Content Queue")
            {
                ((IUIComponent)contentQueuePage).Attach(navigationView);

                if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                    ((IUIComponent)contentQueuePage).SetMargin(new Thickness(50, 10, 20, 20));
                else
                    ((IUIComponent)contentQueuePage).SetMargin(new Thickness(20, 10, 20, 20));
            }
        });
        return navigationView;
    }

    /// <summary>
    /// Called when the application is initialized.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && Entities is not null)
        {

            desktop.MainWindow = Entities["MainWindow"].Get<Window>();
            desktop.MainWindow.Hide();
        }
        base.OnFrameworkInitializationCompleted();

    }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    public void ShowMainWindow()
    {
        if (Entities != null && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
        {
            var mainWindow = Entities["MainWindow"].Get<Window>();
            mainWindow.Show();
        }
    }

    /// <summary>
    /// Shutsdown the the entire app.
    /// </summary>
    public void ShutdownApplication()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void InitializeTrayIcon()
    {
        if (Entities == null) return;

        try
        {
            // Load icon from resources
            var iconStream = AssetLoader.Open(new Uri("avares://Avalonia.Flecs.StellaLearning/Assets/stella-icon.ico"));

            // Create the tray icon
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconStream),
                ToolTipText = "Stella Learning",
                IsVisible = true,
                Menu = new NativeMenu
                {
                    Items =
                    {
                        new NativeMenuItem("Open")
                        {
                            Command = new RelayCommand(() => ShowMainWindow())
                        },
                        new NativeMenuItemSeparator(),
                        new NativeMenuItem("Exit")
                        {
                            Command = new RelayCommand(() => ShutdownApplication())
                        }
                    }
                }
            };

            // Handle click on the tray icon
            _trayIcon.Clicked += (sender, args) => ShowMainWindow();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize tray icon: {ex.Message}");
        }
    }
}