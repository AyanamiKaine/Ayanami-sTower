using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.ToDoApp;

public partial class App : Application
{
    World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();


        //First Defining Entities
        var window = _world.Entity("MainWindow");
        var grid = _world.Entity("Grid");
        var title = _world.Entity("TODO-ListTitle");
        var scrollViewer = _world.Entity("ScrollViewer");
        var itemsController = _world.Entity("ItemsController");
        var textBox = _world.Entity("ItemTextBox");
        var addButton = _world.Entity("AddItemButton");

        window.Set(
                new Window()
                {
                    Title = "Avlonia.Flecs Example",
                    Height = 300,
                    Width = 500,
                    Padding = new Thickness(4),
                }); ;

        grid
            .ChildOf(window)
            .Set(new Grid()
            {
                RowDefinitions = new RowDefinitions("Auto, *, Auto"),
            });


        title
            .ChildOf(grid)
            .Set(new TextBlock()
            {
                Text = "My ToDo-List",
            });

        scrollViewer
            .ChildOf(grid)
            .Set(new ScrollViewer()
            {
            });

        itemsController
            .ChildOf(scrollViewer)
            .Set(new ItemsControl()
            {
            });

        Grid.SetRow(scrollViewer.Get<ScrollViewer>(), 1);

        addButton
            .Set(new Button()
            {
                Content = "Add",
            });



        textBox
            .ChildOf(grid)
            .Set(new TextBox()
            {
                Text = "",
                Watermark = "Add a new Item",
                InnerRightContent = addButton.Get<Button>(),
            });

        textBox.Observe<KeyDown>((Entity e) =>
        {
            if (e.Get<KeyDown>().Args.Key == Key.Enter && textBox.Get<TextBox>().Text != "")
            {
                itemsController.Get<ItemsControl>().Items.Add(textBox.Get<TextBox>().Text);
                textBox.Get<TextBox>().Text = "";
            }
        });

        addButton.Observe<Click>((Entity e) =>
        {
            Console.WriteLine(title.Path());
            if (textBox.Get<TextBox>().Text != "")
            {
                itemsController.Get<ItemsControl>().Items.Add(textBox.Get<TextBox>().Text);
                textBox.Get<TextBox>().Text = "";

                var titleEntityFound = _world.TryLookup(".MainWindow.Grid.TODO-ListTitle", out Entity title);
                if (titleEntityFound)
                {
                    title.Get<TextBlock>().Text = $"My ToDo-List ({itemsController.Get<ItemsControl>().Items.Count})";
                }
            }
        });


        Grid.SetRow(textBox.Get<TextBox>(), 2);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _world.Lookup("MainWindow").Get<Window>();

        }
        this.AttachDevTools();
        base.OnFrameworkInitializationCompleted();
    }
}