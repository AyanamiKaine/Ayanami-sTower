using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Input;
using Avalonia.Layout;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

public class SpacedRepetitionPage
{
    public static Entity Create(World world)
    {
        var spacedRepetitionPage = world.Entity("SpacedRepetitionPage")
            .Add<Page>()
                    .Set(new Grid()
                    {
                        /*

                        *: This represents a "star" column. It means this column will take up as much available space as possible after any fixed-size or Auto columns have been accounted for. Think of it as flexible or "greedy". In this case, the first column will grab most of the grid's width.

                        Auto: This means the column's width will adjust automatically to fit the content within it. If you place a button in this column, the column will be just wide enough to accommodate the button's size.

                        */
                        ColumnDefinitions = new ColumnDefinitions("*, Auto, Auto"),
                        RowDefinitions = new RowDefinitions("Auto, *, Auto"),

                    });

        var listSearchSpacedRepetition = world.Entity("ListSearchSpacedRepetition")
                    .ChildOf(spacedRepetitionPage)
                    .Set(new TextBox()
                    {
                        Watermark = "Search Entries",
                    });

        var totalItems = world.Entity("TotalItems")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBlock()
            {
                Text = "Total Items: 0",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0)
            });

        Grid.SetColumn(totalItems.Get<Control>(), 1);

        var sortItemsButton = world.Entity("SortItemsButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new ComboBox() { });


        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Date");
        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Priority");
        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Name");

        Grid.SetColumn(sortItemsButton.Get<Control>(), 2);

        /*
        I believe that entites should know the exact control type but
        all other entities should only care for the base classes like
        Control, Panel, ItemsControl, TemplatedControl, Etc. They should
        always take the lowest common denominator.

        No need to depend on things that we dont care for 
        */
        ObservableCollection<string> dummyItems =
    [

    ];

        var scrollViewer = world.Entity("ScrollViewer")
            .ChildOf(spacedRepetitionPage)
        .Set(new ScrollViewer()
        {

        });

        var srItems = world.Entity("SpaceRepetitionList")
            .ChildOf(scrollViewer)
            .Set(new ListBox()
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = dummyItems,
            });

        //Use MenuFlyout to create a context menu
        //contextMenu is used for legacy WPF apps
        var contextFlyout = world.Entity("ContextFlyout")
            .ChildOf(spacedRepetitionPage)
            .Set(new MenuFlyout());

        var flyout = contextFlyout.Get<MenuFlyout>();

        flyout.Items.Add(new MenuItem() { Header = "Open" });
        flyout.Items.Add(new MenuItem() { Header = "Edit" });
        flyout.Items.Add(new MenuItem() { Header = "Delete" });


        ((MenuItem)contextFlyout.Get<MenuFlyout>().Items[0]).Click += (sender, e) =>
        {
            Console.WriteLine("Open Clicked");
        };

        ((MenuItem)contextFlyout.Get<MenuFlyout>().Items[1]).Click += (sender, e) =>
        {
            Console.WriteLine("Edit Clicked");
        };

        ((MenuItem)contextFlyout.Get<MenuFlyout>().Items[2]).Click += (sender, e) =>
        {
            Console.WriteLine("Delete Clicked");
        };

        for (int i = 0; i < 100; i++)
        {
            dummyItems.Add($"Item {i}");
        }

        Grid.SetRow(scrollViewer.Get<Control>(), 1);
        //Sets the SCrollViewer to span 3 columns.
        Grid.SetColumnSpan(scrollViewer.Get<Control>(), 3);


        sortItemsButton.Observe<SelectionChanged>((Entity e) =>
        {
            var args = e.Get<SelectionChanged>().Args;
            if (args.AddedItems.Count == 0)
            {
                return;
            }
            var selectedItem = args.AddedItems[0].ToString();
            if (selectedItem == "Sort By Date")
            {

            }
            else if (selectedItem == "Sort By Priority")
            {
                dummyItems = [.. dummyItems.OrderByDescending(s => s)];
                srItems.Get<ItemsControl>().ItemsSource = dummyItems;
            }
            else if (selectedItem == "Sort By Name")
            {
                //(ascending order)
                Random rng = new();
                dummyItems = [.. dummyItems.OrderBy(s => rng.Next())];
                srItems.Get<ItemsControl>().ItemsSource = dummyItems;

            }

        });


        return spacedRepetitionPage;
    }
}
