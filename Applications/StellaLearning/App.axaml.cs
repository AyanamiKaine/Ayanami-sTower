using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS.Events;
using Avalonia.Flecs.FluentUI.Controls;
using System;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform;
using NLog;
using Avalonia.Flecs.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Avalonia.Styling;
using System.IO;
using System.Text.Json;
using System.Linq;
using Avalonia;
using AyanamisTower.StellaLearning.Pages;
using AyanamisTower.StellaLearning.Data;
using AyanamisTower.StellaLearning.Converters;
using AyanamisTower.StellaLearning.Util;


namespace AyanamisTower.StellaLearning;

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
    private ArtPage? artPage;
    private AccountPage? accountPage;
    private IUIComponent? _currentPage;


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

        // Create the scheduler instance (use default parameters or load custom ones)
        var scheduler = new FsrsSharp.Scheduler();
        LargeLanguageManager.Initialize(_world);
        SchedulerService.Initialize(scheduler);

        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();
        _world.Set(LoadSpaceRepetitionItemsFromDisk());

        Dispatcher.UIThread.Post(async () => await StatsTracker.Instance.InitializeAsync(
            [.. _world.Get<ObservableCollection<SpacedRepetitionItem>>().Select(item => item.Uid)]));

        InitializeTrayIcon();
        InitializeSettings();



        MainWindow = _world.UI<Window>((window) =>
        {
            window
                  .AlwaysOnTop(_world.Get<Settings>().EnableAlwaysOnTop)
                  .SetTitle("Stella Learning")
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
    }


    private UIBuilder<NavigationView> CreateUILayout()
    {
        contentQueuePage = new(_world);
        knowledgeVaultPage = new(_world);
        settingsPage = new(_world);
        homePage = new(_world);
        literaturePage = new(_world);
        spacedRepetitionPage = new(_world);
        artPage = new(_world);
        statisticsPage = new(_world);
        accountPage = new(_world);
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


            nav.Child<NavigationViewItem>((item) =>
            {
                item.SetIconSource(new SymbolIconSource()
                {
                    Symbol = Symbol.Account
                });
                item.Child<TextBlock>(t => t.SetText("Account"));
            });

            nav.Child<NavigationViewItem>((item) =>
            {
                item.SetIconSource(new SymbolIconSource()
                {
                    Symbol = Symbol.Edit
                });
                item.Child<TextBlock>(t => t.SetText("Art"));
            });

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

            nav.Child<NavigationViewItem>((item) =>
            {
                item.SetIconSource(new SymbolIconSource()
                {
                    Symbol = Symbol.Bookmarks
                });
                item.Child<TextBlock>(t => t.SetText("Literature"));
            });

            var srPage = nav.Child<NavigationViewItem>((item) =>
            {
                item.SetIconSource(new SymbolIconSource()
                {
                    Symbol = Symbol.RepeatAll
                });
                item.Child<TextBlock>(t => t.SetText("Spaced Repetition"));
            });

            nav.Child<NavigationViewItem>((item) =>
            {
                item.SetIconSource(new SymbolIconSource()
                {
                    Symbol = Symbol.Library
                });
                item.Child<TextBlock>(t => t.SetText("Statistics"));
            });
            nav.Get<NavigationView>().SelectedItem = srPage.Get<NavigationViewItem>();
            nav.Child(spacedRepetitionPage);
            _currentPage = spacedRepetitionPage;
            nav.OnDisplayModeChanged((sender, args) =>
            {

                if (_currentPage is null)
                    return;

                _currentPage.Root.Get<Control>().Margin = args.DisplayMode == NavigationViewDisplayMode.Minimal
                    ? new Thickness(50, 7, 20, 20)
                    : new Thickness(20, 5, 20, 20);
            });
            // This observer runs its callback when 
            // nav.Emit<OnSelectionChanged>() is called.
            nav.Observe<OnSelectionChanged>(_ =>
            {
                // We first remove any other page ensuring 
                // that only the selected page is displayed
                nav.Children(child =>
                {
                    if (child.Has<Avalonia.Flecs.Controls.ECS.Module.Page>())
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
                    nav.Child(homePage);
                    _currentPage = homePage;
                    ApplyPageMargin(homePage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock LiteratureTextBlock && LiteratureTextBlock.Text == "Literature")
                {
                    nav.Child(literaturePage);
                    _currentPage = literaturePage;
                    ApplyPageMargin(literaturePage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock StatisticsTextBlock && StatisticsTextBlock.Text == "Statistics")
                {
                    nav.Child(statisticsPage);
                    _currentPage = statisticsPage;
                    ApplyPageMargin(statisticsPage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock SRtextBlock && SRtextBlock.Text == "Spaced Repetition")
                {
                    nav.Child(spacedRepetitionPage);
                    _currentPage = spacedRepetitionPage;
                    ApplyPageMargin(spacedRepetitionPage, e.DisplayMode);
                }
                else if (selectedItem?.Content is not null && selectedItem?.Content.ToString() == "Settings")
                {
                    nav.Child(settingsPage);
                    _currentPage = settingsPage;
                    ApplyPageMargin(settingsPage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock KVtextBlock && KVtextBlock.Text == "Knowledge Vault")
                {
                    nav.Child(knowledgeVaultPage);
                    _currentPage = knowledgeVaultPage;
                    ApplyPageMargin(knowledgeVaultPage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock CQtextBlock && CQtextBlock.Text == "Content Queue")
                {
                    nav.Child(contentQueuePage);
                    _currentPage = contentQueuePage;
                    ApplyPageMargin(contentQueuePage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock artTextBlock && artTextBlock.Text == "Art")
                {
                    nav.Child(artPage);
                    _currentPage = artPage;
                    ApplyPageMargin(artPage, e.DisplayMode);
                }
                else if (selectedItem?.Content is TextBlock accountTextBlock && accountTextBlock.Text == "Account")
                {
                    nav.Child(accountPage);
                    _currentPage = accountPage;
                    ApplyPageMargin(accountPage, e.DisplayMode);
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

            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.Show();
                mainWindow.Activate();
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

            Dispatcher.UIThread.Post(async () => await StatsTracker.Instance.SaveStatsAsync());
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
            else if (e.PropertyName == nameof(Settings.EnableAlwaysOnTop))
            {
                // Ensure _mainWindow exists and update runs on UI thread
                if (_mainWindow != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Directly set the Topmost property on the actual Window instance
                        _mainWindow.Topmost = settings.EnableAlwaysOnTop;
                        Logger.Trace($"MainWindow Topmost property set to: {settings.EnableAlwaysOnTop}"); // Optional: Add logging
                    });
                }
                else
                {
                    Logger.Warn("Attempted to update Topmost, but _mainWindow is null."); // Optional: Add logging
                }
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
                            Command = new RelayCommand(ShowMainWindow)
                        },
                        new NativeMenuItemSeparator(),
                        new NativeMenuItem("Exit")
                        {
                            Command = new RelayCommand(ShutdownApplication)
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

    /// <summary>
    /// Loads the spaced repetition items from disk
    /// </summary>
    /// <returns></returns>
    public static ObservableCollection<SpacedRepetitionItem> LoadSpaceRepetitionItemsFromDisk()
    {
        string filePath = Path.Combine("./save", "space_repetition_items.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);

            // Create JsonSerializerOptions and register the converter
            var options = new JsonSerializerOptions
            {
                Converters = { new SpacedRepetitionItemConverter() }
            };

            ObservableCollection<SpacedRepetitionItem>? items = JsonSerializer.Deserialize<ObservableCollection<SpacedRepetitionItem>>(jsonString, options);

            foreach (var item in items!)
            {
                item.CreateCardFromSpacedRepetitionData();
            }

            return items ?? [];
        }
        else
        {
            return [];
        }
    }

    private static void ApplyPageMargin(IUIComponent page, NavigationViewDisplayMode displayMode)
    {
        // Check if the page's root entity is still valid before accessing it
        if (!page.Root.IsAlive())
        {
            Logger.Warn($"ApplyPageMargin: Page root entity {page.Root.Id} is not alive. Skipping margin set.");
            return;
        }

        // Check if the page root actually has a Control component
        if (!page.Root.Has<Control>())
        {
            Logger.Warn($"ApplyPageMargin: Page root entity {page.Root.Id} does not have a Control component. Skipping margin set.");
            return;
        }

        var margin = displayMode == NavigationViewDisplayMode.Minimal
            ? new Thickness(50, 7, 20, 20)
            : new Thickness(20, 5, 20, 20);

        Logger.Trace($"Applying margin {margin} to page {page.Root.Id} for display mode {displayMode}");
        page.SetMargin(margin);
    }
}