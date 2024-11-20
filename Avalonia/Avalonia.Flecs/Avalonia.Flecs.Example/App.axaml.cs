using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Example;

public partial class App : Application
{
    World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var window = _world.Entity("MainWindow")
            .Set(
                new Window()
                {
                    Title = "Avlonia.Flecs Example"
                });

        var textBlock = _world.Entity("HelloWorldTextBlock")
            .ChildOf(window)
            .Set(
                new TextBlock()
                {
                    Text = "Avalonia + Flecs = ♥"
                });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _world.Lookup("MainWindow").Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}