using Avalonia;
using Avalonia.Controls;
using Avalonia.Flecs.FluentUI.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.Controls;
using Avalonia.Markup.Xaml;
using AyanamisTower.Toast;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace AyanamisTower.SpacetimeDBViewer;

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

    private readonly World _world = World.Create();
    //private IUIComponent? _currentPage;

    /// <summary>
    /// Ui Builder that represents the main window
    /// </summary>
    public UIBuilder<Window>? MainWindow;

    /// <summary>
    /// Initializes the application.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Create the scheduler instance (use default parameters or load custom ones)
        var scheduler = new FsrsSharp.Scheduler();
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        MainWindow = _world.UI<Window>(
            (window) =>
            {
                window
                    .SetTitle("Spacetime DB Viewer")
                    .SetHeight(400)
                    .SetWidth(400);
            }
        );
        _mainWindow = MainWindow.Get<Window>();

        MainWindow.Child<Grid>((grid) =>
        {
            grid.Child(CreateUILayout());


            /*
            Here we are setting up where our toasts are shown
            */

            grid.Child<StackPanel>((stack) =>
            {

                /*
                TODO:
                I think its really important to add an ability for users 
                to customize this styling. Like where the toast should 
                disapear.
                */

                stack
                    .SetSpacing(5)
                    .SetOrientation(Avalonia.Layout.Orientation.Vertical)
                    .SetVerticalAlignment(Avalonia.Layout.VerticalAlignment.Bottom)
                    .SetHorizontalAlignment(Avalonia.Layout.HorizontalAlignment.Right)
                    .SetMaxWidth(400)
                    .SetMargin(10);

                ToastService.Initialize(stack.Get<StackPanel>());
            });
        });
    }

    private UIBuilder<NavigationView> CreateUILayout()
    {
        // Using the UI Builder is part of an effort to make it much more obvious how the UI is structured.
        // While you might say just use XAML, the whole point was not to use it in the firstplace.

        return _world.UI<NavigationView>(nav =>
        {
            nav.SetPaneTitle("Spacetime DB Viewer").SetColumn(0);
        });
    }

    /// <summary>
    /// Called when the application is initialized.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        //_world.RunRESTAPI();
#if DEBUG
        this.AttachDevTools();
#endif
        _mainWindow?.Show();
    }
}
