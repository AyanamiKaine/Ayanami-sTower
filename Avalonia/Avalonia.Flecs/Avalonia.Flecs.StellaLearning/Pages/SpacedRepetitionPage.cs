using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Layout;
using Flecs.NET.Core;

using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

static public class SpacedRepetitionPage
{
    public static Entity Create(World world)
    {
        var spacedRepetitionPage = world.Entity("SpacedRepetitionPage")
            .Add<Page>()
            .Set(new Grid())
            /*
            *: This represents a "star" column. 
            It means this column will take up as much available space as 
            possible after any fixed-size or Auto columns have been accounted for. 
            Think of it as flexible or "greedy". 
            In this case, the first column will grab most of the grid's width.

            Auto: This means the column's width will adjust automatically to 
            fit the content within it. If you place a button in this column, 
            the column will be just wide enough to accommodate the button's size.
            */
            .SetColumnDefinitions(new ColumnDefinitions("*, Auto, Auto"))
            .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));

        var listSearchSpacedRepetition = world.Entity("ListSearchSpacedRepetition")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBox())
            .SetColumn(0)
            .SetWatermark("Search Entries");

        var totalItems = world.Entity("TotalItems")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBlock())
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetMargin(new Thickness(10, 0))
            .SetText("Total Items: 0")
            .SetColumn(1);

        List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];
        var sortItemsButton = world.Entity("SortItemsButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new ComboBox())
            .SetPlaceholderText("Sort Items")
            .SetColumn(2)
            .SetItemsSource(sortItems);

        /*
        I believe that entites should not know the exact control type but
        all other entities should only care for the base classes like
        Control, Panel, ItemsControl, TemplatedControl, Etc. They should
        always take the lowest common denominator.

        No need to depend on things that we dont care for 
        */


        var scrollViewer = world.Entity("ScrollViewer")
            .ChildOf(spacedRepetitionPage)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        ObservableCollection<string> dummyItems = [];
        var srItems = world.Entity("SpaceRepetitionList")
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(dummyItems)
            .SetSelectionMode(SelectionMode.Multiple);

        listSearchSpacedRepetition.OnTextChanged((sender, args) =>
        {
            string searchText = listSearchSpacedRepetition.Get<TextBox>().Text!.ToLower();
            var filteredItems = dummyItems.Where(item => item.ToLower().Contains(searchText));
            //srItems.Get<ListBox>().ItemsSource = new ObservableCollection<string>(filteredItems);
            srItems.SetItemsSource(new ObservableCollection<string>(filteredItems));
        });

        //Use MenuFlyout to create a context menu
        //contextMenu is used for legacy WPF apps
        var contextFlyout = world.Entity("ContextFlyout")
            .ChildOf(spacedRepetitionPage)
            .Set(new MenuFlyout());


        var openMenuItem = world.Entity("OpenMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("Open")
            .OnClick(async (sender, args) =>
            {
                await ScriptExample.RunScriptAsync(world);
            });

        var editMenuItem = world.Entity("EditMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("Edit")
            .OnClick((sender, args) =>
            {
                Console.WriteLine("Edit Clicked");
            }); ;

        var deleteMenuItem = world.Entity("DeleteMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("Delete")
            .OnClick((sender, args) =>
            {
                Console.WriteLine("Delete Clicked");
            });

        dummyItems.Add("algorithm");
        dummyItems.Add("binary");
        dummyItems.Add("complexity");
        dummyItems.Add("data structure");
        dummyItems.Add("efficiency");
        dummyItems.Add("Fibonacci");
        dummyItems.Add("graph");
        dummyItems.Add("hash table");
        dummyItems.Add("iteration");
        dummyItems.Add("JavaScript");

        scrollViewer
            .SetRow(1)
            .SetColumnSpan(3);


        sortItemsButton.OnSelectionChanged((sender, args) =>
        {
            if (args.AddedItems.Count == 0)
            {
                return;
            }
            var selectedItem = args.AddedItems[0]!.ToString();
            if (selectedItem == "Sort By Date")
            {

            }
            else if (selectedItem == "Sort By Priority")
            {
                var t = (ObservableCollection<string>)srItems.GetItemsSource()!;
                t = [.. t!.OrderByDescending(s => s)];
                srItems.SetItemsSource(t);
            }
            else if (selectedItem == "Sort By Name")
            {
                //(ascending order)
                Random rng = new();
                var t = (ObservableCollection<string>)srItems.GetItemsSource()!;
                t = [.. t!.OrderBy(s => rng.Next())];
                srItems.SetItemsSource(t);
            }
        });

        return spacedRepetitionPage;
    }
}
