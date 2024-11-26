using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Pages;

public static class SpacedRepetitionPage
{
    public static Entity Create(World world)
    {
        var spacedRepetitionPage = world.Entity("SpacedRepetitionPage")
            .Add<Page>()
            .Set(new Grid()
            {
                Margin = new Thickness(10),
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

        Grid.SetColumn(totalItems.Get<TextBlock>(), 1);

        var sortItemsButton = world.Entity("SortItemsButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new ComboBox()
            {

            });

        Grid.SetColumn(sortItemsButton.Get<ComboBox>(), 2);

        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Date");
        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Priority");

        var srcsrollViewer = world.Entity("SpaceRepetitionScrollViewer")
            .ChildOf(spacedRepetitionPage)
            .Set<ScrollViewer>(new ScrollViewer());

        var srItems = world.Entity("SpaceRepetitionList")
            .ChildOf(srcsrollViewer)
            .Set<ItemsControl>(new ItemsControl());

        Grid.SetRow(srcsrollViewer.Get<ScrollViewer>(), 1);



        return spacedRepetitionPage;
    }
}
