using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;

namespace Avalonia.Flecs.ToDoApp;

/// <summary>
/// This class contains extension methods for the int type.
/// It adds a method to check if a number is even.
/// C# can be extended with extension methods.
/// </summary>
public static class IntExtensions
{
    public static bool IsEven(this int number)
    {
        return number % 2 == 0;
    }
}
public partial class App : Application
{
    public class TodoItem(string text)
    {
        public string Text { get; set; } = text;
        public bool IsDone { get; set; } = false;

        public override string ToString()
        {
            return Text + (IsDone ? " (Done)" : "(Not Done)");
        }
    }

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

        window
            .Set(new Window())
            .SetWindowTitle("Avalonia.Flecs.ToDoApp")
            .SetHeight(300)
            .SetWidth(500)
            .SetPadding(new Thickness(4));

        grid
            .ChildOf(window)
            .Set(new Grid())
            .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));

        title
            .ChildOf(grid)
            .Set(new TextBlock())
            .SetText("My ToDo-List");

        scrollViewer
            .ChildOf(grid)
            .Set(new ScrollViewer());

        /*
        This creates a template for the TodoItem class.
        It defines how this class should be displayed in the ItemsControl.
        */
        var template = new FuncDataTemplate<TodoItem>((value, namescope) =>
        {
            var grid = new Grid();
            /*

            *: This represents a "star" column. It means this column will take up as much available space as possible after any fixed-size or Auto columns have been accounted for. Think of it as flexible or "greedy". In this case, the first column will grab most of the grid's width.

            Auto: This means the column's width will adjust automatically to fit the content within it. If you place a button in this column, the column will be just wide enough to accommodate the button's size.

            */
            grid.ColumnDefinitions = new ColumnDefinitions("*, Auto");
            var checkBox = new CheckBox()
            {
                [!CheckBox.ContentProperty] = new Binding("Text"),
            };
            checkBox.IsChecked = value.IsDone;

            var button = new Button()
            {
                Content = "Delete",
            };

            button.Click += (sender, e) =>
            {
                itemsController.Get<ItemsControl>().Items.Remove(value);
                var titleEntityFound = _world.TryLookup(".MainWindow.Grid.TODO-ListTitle", out Entity title);
                if (titleEntityFound)
                {
                    title.SetText($"My ToDo-List ({itemsController.Get<ItemsControl>().Items.Count})");
                }
            };
            Grid.SetColumn(button, 1);
            grid.Children.Add(checkBox);
            grid.Children.Add(button);

            return grid;
        });

        itemsController
            .ChildOf(scrollViewer)
            .Set(new ItemsControl()
            {
                ItemTemplate = template
            });

        Grid.SetRow(scrollViewer.Get<ScrollViewer>(), 1);

        addButton
            .Set(new Button())
            .SetContent("Add");

        textBox
            .ChildOf(grid)
            .Set(new TextBox())
            .SetInnerRightContent(addButton.Get<Button>())
            .SetText("")
            .SetWatermark("Add a new Item")
            .OnKeyDown((sender, args) =>
            {
                if (args.Key == Key.Enter && textBox.Get<TextBox>().Text != "")
                {
                    itemsController.Get<ItemsControl>().Items.Add(new TodoItem(textBox.Get<TextBox>().Text!));
                    textBox.SetText("");
                }
            });

        addButton.OnClick((sender, args) =>
        {
            Console.WriteLine(title.Path());
            if (textBox.GetText() != "")
            {
                itemsController.Get<ItemsControl>().Items.Add(new TodoItem(textBox.GetText()));
                textBox.SetText("");

                var titleEntityFound = _world.TryLookup(".MainWindow.Grid.TODO-ListTitle", out Entity title);
                if (titleEntityFound)
                {
                    title.Get<TextBlock>().Text = $"My ToDo-List ({itemsController.Get<ItemsControl>().Items.Count})";
                }
            }
        });

        textBox.SetRow(2);
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