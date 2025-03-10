using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
using Avalonia.Flecs.FluentUI.Controls;
using Avalonia.Flecs.Util;
using Avalonia.Flecs.StellaLearning.Pages;
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

        // Using the UI Builder is part of an effort to make it much more obvious how the UI is structured.
        // While you might say just use XAML, the whole point was not to use it in the firstplace.

        var navigationView = _world.UI<NavigationView>(nav =>
        {
            nav.SetProperty("PaneTitle", "Stella Learning")
               .SetColumn(0);

            nav.Child<ScrollViewer>(scroll =>
            {
                scroll.Child<StackPanel>(stack =>
                {
                    stack.Child<Grid>(grid =>
                    {
                        grid.SetColumnDefinitions("2,*,*")
                            .SetRowDefinitions("Auto");
                    });
                });
            });

            nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(textblock =>
            {
                textblock.SetText("Home");
            }));

            nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(textblock =>
            {
                textblock.SetText("Knowledge Vault");
            }));

            nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(textblock =>
            {
                textblock.SetText("Literature");
            }));

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

            nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(textblock =>
            {
                textblock.SetText("Study");
            }));

            nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(textblock =>
            {
                textblock.SetText("Spaced Repetition");
            }));

            nav.Child(spacedRepetitionPage);

            nav.OnDisplayModeChanged((sender, args) =>
            {
                if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
                {
                    nav.Children((Entity child) =>
                    {
                        if (child.Has<Controls.ECS.Module.Page>() && child.Has<Control>())
                        {
                            child.Get<Control>().Margin = new Thickness(50, 10, 20, 20);
                        }
                    });
                }
                else if (args.DisplayMode == NavigationViewDisplayMode.Compact)
                {
                    nav.Children((Entity child) =>
                    {
                        if (child.Has<Controls.ECS.Module.Page>() && child.Has<Control>())
                        {
                            child.Get<Control>().Margin = new Thickness(20, 10, 20, 20);
                        }
                    });
                }
            });

            nav.Observe<OnSelectionChanged>((Entity _) =>
                    {
                        // We first remove any other page ensuring 
                        // that only the selected page is displayed
                        nav.Children((Entity child) =>
                            {
                                if (child.Has<Controls.ECS.Module.Page>())
                                {
                                    child.Remove(Ecs.ChildOf, Ecs.Wildcard);
                                }
                            });
                    });

            nav.OnNavViewSelectionChanged(async (sender, args) =>
            {
                await Threading.Dispatcher.UIThread.InvokeAsync(() => nav.Emit<OnSelectionChanged>());

                if (sender is not NavigationView e)
                    return;

                var selectedItem = args.SelectedItem as NavigationViewItem;
                if (selectedItem?.Content is TextBlock homeTextBlock && homeTextBlock.Text == "Home")
                {
                    nav.Child(homePage);

                    //Maybe we could implement an event for the navigation view entity that says
                    //something like new page added and than changes the margin of the page
                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)homePage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)homePage).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock LiteratureTextBlock && LiteratureTextBlock.Text == "Literature")
                {
                    nav.Child(literaturePage);
                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)literaturePage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)literaturePage).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock SRtextBlock && SRtextBlock.Text == "Spaced Repetition")
                {
                    nav.Child(spacedRepetitionPage);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)spacedRepetitionPage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)spacedRepetitionPage).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Settings")
                {
                    nav.Child(settingsPage);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)settingsPage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)settingsPage).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock KVtextBlock && KVtextBlock.Text == "Knowledge Vault")
                {
                    nav.Child(knowledgeVaultPage);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)knowledgeVaultPage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)knowledgeVaultPage).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock CQtextBlock && CQtextBlock.Text == "Content Queue")
                {
                    nav.Child(contentQueuePage);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)contentQueuePage).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)contentQueuePage).SetMargin(new Thickness(20, 10, 20, 20));
                }
            });

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
            var iconStream = AssetLoader.Open(new Uri("avares://Stella Learning/Assets/stella-icon.ico"));

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