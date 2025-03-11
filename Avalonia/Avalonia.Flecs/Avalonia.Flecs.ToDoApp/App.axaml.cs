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
    Entity _mainWindow;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        _mainWindow = _world.UI<Window>((window) =>
        {
            UIBuilder<ItemsControl>? itemsController = null;
            UIBuilder<TextBlock>? title = null;
            window.SetTitle("Avalonia.Flecs.ToDoAapp")
                  .SetHeight(300)
                  .SetWidth(500)
                  .SetPadding(new Thickness(4))
                  .Child<Grid>((grid) =>
                    {
                        grid.SetRowDefinitions("Auto, *, Auto");
                        grid.Child<TextBlock>((textBlock) =>
                        {
                            title = textBlock;
                            textBlock.SetText("My ToDo-List (0)");
                        });
                        grid.Child<ScrollViewer>(scrollViewer =>
                        {
                            scrollViewer.SetRow(1);
                            scrollViewer.Child<ItemsControl>((itemsControl) =>
                            {
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
                                        itemsControl.Entity.Get<ItemsControl>().Items.Remove(value);
                                        title!.SetText($"My ToDo-List ({itemsController!.GetItems().Count})");
                                    };
                                    Grid.SetColumn(button, 1);
                                    grid.Children.Add(checkBox);
                                    grid.Children.Add(button);

                                    return grid;
                                });

                                itemsControl.SetItemTemplate(template);
                                itemsController = itemsControl;
                            });
                        });
                        grid.Child<TextBox>((textBox) =>
                        {
                            var addButton = _world.UI<Button>((button) =>
                            {
                                button.Child<TextBlock>((textBlock) => textBlock.SetText("Add"));
                                button.OnClick((sender, args) =>
                                    {
                                        if (textBox.GetText() != "")
                                        {
                                            itemsController!.GetItems().Add(new TodoItem(textBox.GetText()));
                                            textBox.SetText("");
                                            title!.SetText($"My ToDo-List ({itemsController!.GetItems().Count})");
                                        }
                                    });
                            });

                            textBox.SetInnerRightContent(addButton);
                            textBox.SetRow(2);
                            textBox.SetText("");
                            textBox.SetWatermark("Add a new Item");
                            textBox.OnKeyDown((sender, args) =>
                            {
                                // This is quite combersome to do.
                                // In our hierarchy the needed ui entity that has 
                                // the items controller is in another node of the UI tree. 
                                // For now we can simply store a refrence to the UI builder
                                // itemsControl
                                if (args.Key == Key.Enter && textBox.Get<TextBox>().Text != "")
                                {
                                    itemsController!.Get<ItemsControl>().Items.Add(new TodoItem(textBox.Get<TextBox>().Text!));
                                    textBox.SetText("");
                                    title!.SetText($"My ToDo-List ({itemsController!.GetItems().Count})");
                                }
                            });
                        });
                    });
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _mainWindow.Get<Window>();
        }
        base.OnFrameworkInitializationCompleted();
#if DEBUG
        this.AttachDevTools();
#endif

    }
}