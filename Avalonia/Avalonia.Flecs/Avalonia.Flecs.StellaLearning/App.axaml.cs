using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
using Avalonia.Flecs.FluentUI.Controls;
using Avalonia.Flecs.StellaLearning.Pages;
using System;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform;
using NLog;
using Avalonia.Flecs.Controls;
using Avalonia.Threading;
using Avalonia.Flecs.StellaLearning.Data;
using System.Collections.ObjectModel;
using Avalonia.Styling;

namespace Avalonia.Flecs.StellaLearning;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private static Window? _mainWindow;
    /// <summary>
    /// Returns the MainWindow
    /// </summary>
    /// <returns></returns>
    public static Window GetMainWindow() => _mainWindow!;
    private World _world = World.Create();

    private ContentQueuePage? contentQueuePage;
    private KnowledgeVaultPage? knowledgeVaultPage;
    private SettingsPage? settingsPage;
    private HomePage? homePage;
    private LiteraturePage? literaturePage;
    private SpacedRepetitionPage? spacedRepetitionPage;
    private StatisticsPage? statisticsPage;

    /// <summary>
    /// Ui Builder that represents the main window
    /// </summary>
    public UIBuilder<Window>? MainWindow;

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

        _world.Set<ObservableCollection<SpacedRepetitionItem>>([]);
        //var debugWindow = new Debug.Window.Window(_world);
        //Entities.OnEntityAdded += debugWindow.AddEntity;

        Dispatcher.UIThread.Post(async () =>
        {
            await StatsTracker.Instance.InitializeAsync();
        });

        InitializeSettings();

        MainWindow = _world.UI<Window>((window) =>
            {
                window.SetTitle("Stella Learning")
                      .SetHeight(400)
                      .SetWidth(400)
                      .OnClosing((sender, args) =>
                      {
                          if (sender is Window win)
                          {
                              // We dont close the main window but instead hide it,
                              // because we have a tray icon that is still active.
                              if (_world.Get<Settings>().CloseToTray)
                              {
                                  args.Cancel = true;
                                  win.Hide();
                              }
                              else
                              {
                                  args.Cancel = false;
                              }
                          }
                      });
            });
        _mainWindow = MainWindow.Get<Window>();

        MainWindow.Child(CreateUILayout());
        InitializeTrayIcon();
    }


    private UIBuilder<NavigationView> CreateUILayout()
    {
        contentQueuePage = new(_world);
        knowledgeVaultPage = new(_world);
        settingsPage = new(_world);
        homePage = new(_world);
        literaturePage = new(_world);
        spacedRepetitionPage = new(_world);
        statisticsPage = new(_world);

        // Using the UI Builder is part of an effort to make it much more obvious how the UI is structured.
        // While you might say just use XAML, the whole point was not to use it in the firstplace.

        return _world.UI<NavigationView>(nav =>
        {
            nav.SetPaneTitle("Stella Learning")
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

            //nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(t => t.SetText("Home")));
            //nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(t => t.SetText("Knowledge Vault")));
            //nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(t => t.SetText("Literature")));
            //nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(t => t.SetText("Content Queue")));

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

            //nav.Child<NavigationViewItem>(item => item.Child<TextBlock>(t => t.SetText("Study")));
            var srPage = nav.Child<NavigationViewItem>((item) =>
            {
                item.With((item) =>
                {
                    item.IconSource = new SymbolIconSource()
                    {
                        Symbol = Symbol.RepeatAll
                    };
                });
                item.Child<TextBlock>(t => t.SetText("Spaced Repetition"));
            });

            nav.Child<NavigationViewItem>((item) =>
            {
                item.With((item) =>
                {
                    item.IconSource = new SymbolIconSource()
                    {
                        Symbol = Symbol.Library
                    };
                });
                item.Child<TextBlock>(t => t.SetText("Statistics"));

            });
            nav.Get<NavigationView>().SelectedItem = srPage.Get<NavigationViewItem>();
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
                else
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
            // This observer runs its callback when 
            // nav.Emit<OnSelectionChanged>() is called.
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

            // TODO: they way we use a chain if statement is rather ugly, and need improvment.
            nav.OnNavViewSelectionChanged((sender, args) =>
            {
                nav.EmitAsync<OnSelectionChanged>();

                if (sender is not NavigationView e)
                    return;

                var selectedItem = args.SelectedItem as NavigationViewItem;
                if (selectedItem?.Content is TextBlock homeTextBlock && homeTextBlock.Text == "Home")
                {
                    nav.Child(homePage!);

                    //Maybe we could implement an event for the navigation view entity that says
                    //something like new page added and than changes the margin of the page
                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)homePage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)homePage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock LiteratureTextBlock && LiteratureTextBlock.Text == "Literature")
                {
                    nav.Child(literaturePage!);
                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)literaturePage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)literaturePage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock StatisticsTextBlock && StatisticsTextBlock.Text == "Statistics")
                {
                    nav.Child(statisticsPage);
                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)literaturePage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)literaturePage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock SRtextBlock && SRtextBlock.Text == "Spaced Repetition")
                {
                    nav.Child(spacedRepetitionPage!);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)spacedRepetitionPage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)spacedRepetitionPage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Settings")
                {
                    nav.Child(settingsPage!);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)settingsPage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)settingsPage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock KVtextBlock && KVtextBlock.Text == "Knowledge Vault")
                {
                    nav.Child(knowledgeVaultPage!);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)knowledgeVaultPage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)knowledgeVaultPage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
                else if (selectedItem?.Content is TextBlock CQtextBlock && CQtextBlock.Text == "Content Queue")
                {
                    nav.Child(contentQueuePage!);

                    if (e.DisplayMode == NavigationViewDisplayMode.Minimal)
                        ((IUIComponent)contentQueuePage!).SetMargin(new Thickness(50, 10, 20, 20));
                    else
                        ((IUIComponent)contentQueuePage!).SetMargin(new Thickness(20, 10, 20, 20));
                }
            });
        });
    }

    /// <summary>
    /// Called when the application is initialized.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {

        var isDarkMode = _world.Get<Settings>().IsDarkMode;

        if (isDarkMode)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && MainWindow is not null)
        {
            _mainWindow = MainWindow.Get<Window>();
            desktop.MainWindow = MainWindow.Get<Window>();
            desktop.MainWindow.Hide();
        }

        // Create the scheduler instance (use default parameters or load custom ones)
        var scheduler = new FsrsSharp.Scheduler();

        // Initialize the service
        SchedulerService.Initialize(scheduler);

        base.OnFrameworkInitializationCompleted();
        //_world.RunRESTAPI();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    public void ShowMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime && MainWindow is not null)
        {
            var mainWindow = MainWindow.Get<Window>();

            Dispatcher.UIThread.Post(async () =>
            {
                mainWindow.Show();
                mainWindow.Activate();
                await StatsTracker.Instance.InitializeAsync();
            });
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

            Dispatcher.UIThread.Post(async () => await StatsTracker.Instance.InitializeAsync());
        }
    }

    private void InitializeSettings()
    {




        _world.Set(Settings.LoadFromDisk());
        var settings = _world.Get<Settings>();
        settings.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Settings.IsDarkMode))
            {
                if (Application.Current is not null)
                    Settings.SetTheme(Application.Current, settings.IsDarkMode ? "Dark" : "Light");

            }

            Settings.SaveToDisk(settings);
        };
    }

    private void InitializeTrayIcon()
    {
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