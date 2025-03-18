using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;

namespace Avalonia.Flecs.Example;

public partial class App : Application
{
    World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var mainWindow = _world.UI<Window>(window =>
        {
            window.SetTitle("Avlonia.Flecs Example")
                  .SetHeight(400)
                  .SetWidth(400);

            window.Child<DockPanel>(dockPanel =>
            {
                // Add menu at the top of the DockPanel
                dockPanel.Child<Menu>(menu =>
                {
                    menu.SetDock(Dock.Top);

                    menu.Child<MenuItem>(fileMenuItem =>
                    {
                        fileMenuItem.SetHeader("_File");
                    });

                    menu.Child<MenuItem>(editMenuItem =>
                    {
                        editMenuItem.SetHeader("_Edit");
                    });

                    menu.Child<MenuItem>(selectionMenuItem =>
                    {
                        selectionMenuItem.SetHeader("_Selection");
                    });
                });

                // Add text block
                dockPanel.Child<TextBlock>(textBlock =>
                {
                    textBlock.SetText("Hello World!")
                             .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                             .SetHorizontalAlignment(Layout.HorizontalAlignment.Center);
                });

                // Add button at the bottom
                dockPanel.Child<Button>(button =>
                {
                    button.SetDock(Dock.Bottom)
                          .With(btn => btn.Content = "Click Me!")
                          .OnClick((sender, args) =>
                          {
                              if (sender is Button b)
                              {
                                  b.Content = "CHANGED CONTENT";
                                  System.Console.WriteLine("THE BUTTON WAS CLICK WHOAH!");
                              }
                          });
                });
            });
        });

        _world.Entity("MainWindow").Set(mainWindow.Get<Window>());
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