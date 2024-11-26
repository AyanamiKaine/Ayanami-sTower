using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Flecs.NET.Core;
using FlecsCore = Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.StellaLearning;

public interface IUIComponent
{
    /// <summary>
    /// Creates an Entity UI Component that represents a UI Component.
    /// Similar to an react component, it is a function that returns an Entity.
    /// </summary>
    /// <param name="world">Flecs ECS World</param>
    /// <param name="props">Used to pass down data, similar to props in react</param>
    /// <returns></returns>
    Entity Create(World world, params object[] props);
}



public partial class App : Application
{

    private World _world = World.Create();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();
        _world.Observer();

        var window = _world.Entity("MainWindow")
            .Set(
                new Window()
                {
                    Title = "Stella Learning",
                    Height = 400,
                    Width = 400
                });



        var navigationView = _world.Entity("NavigationView")
                    .ChildOf(window)
                    .Set(new NavigationView()
                    {
                        PaneTitle = "Stella Learning",
                    });


        var scrollViewer = _world.Entity("ScrollViewer")
            .ChildOf(navigationView)
            .Set(new ScrollViewer());

        var stackPanel = _world.Entity("StackPanel")
            .ChildOf(scrollViewer)
            .Set(new StackPanel());

        var grid = _world.Entity("MainContentDisplay")
            .ChildOf(stackPanel)
            .Set(new Grid()
            {
                ColumnDefinitions = new ColumnDefinitions("2,*,*"),
                RowDefinitions = new RowDefinitions("Auto")
            });

        var settingPage = Pages.SettingsPage.Create(_world);

        Grid.SetRow(settingPage.Get<TextBlock>(), 2);
        Grid.SetColumnSpan(settingPage.Get<TextBlock>(), 3);

        var homePage = Pages.HomePage.Create(_world);
        Grid.SetRow(homePage.Get<TextBlock>(), 2);
        Grid.SetColumnSpan(homePage.Get<TextBlock>(), 3);

        var literaturePage = _world.Entity("LiteraturePage")
            .ChildOf(navigationView)
            .Set(new TextBlock()
            {
                Text = "Literature",
                Margin = new Thickness(10)
            });

        Grid.SetRow(literaturePage.Get<TextBlock>(), 2);
        Grid.SetColumnSpan(literaturePage.Get<TextBlock>(), 3);


        var spacedRepetitionPage = _world.Entity("SpacedRepetitionPage")
            .Set(new Grid()
            {
                Margin = new Thickness(10),
                /*
                
                *: This represents a "star" column. It means this column will take up as much available space as possible after any fixed-size or Auto columns have been accounted for. Think of it as flexible or "greedy". In this case, the first column will grab most of the grid's width.
                
                Auto: This means the column's width will adjust automatically to fit the content within it. If you place a button in this column, the column will be just wide enough to accommodate the button's size.

                */
                ColumnDefinitions = new ColumnDefinitions("*, Auto, Auto"),
                RowDefinitions = new RowDefinitions("Auto, *, Auto"),

            }).ChildOf(navigationView);

        var listSearchSpacedRepetition = _world.Entity("ListSearchSpacedRepetition")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBox()
            {
                Watermark = "Search Entries",
            });

        var totalItems = _world.Entity("TotalItems")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBlock()
            {
                Text = "Total Items: 0",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0)
            });

        Grid.SetColumn(totalItems.Get<TextBlock>(), 1);

        var sortItemsButton = _world.Entity("SortItemsButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new ComboBox()
            {

            });

        Grid.SetColumn(sortItemsButton.Get<ComboBox>(), 2);

        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Date");
        sortItemsButton.Get<ComboBox>().Items.Add("Sort By Priority");

        var srcsrollViewer = _world.Entity("SpaceRepetitionScrollViewer")
            .ChildOf(spacedRepetitionPage)
            .Set<ScrollViewer>(new ScrollViewer());

        var srItems = _world.Entity("SpaceRepetitionList")
            .ChildOf(srcsrollViewer)
            .Set<ItemsControl>(new ItemsControl());

        Grid.SetRow(srcsrollViewer.Get<ScrollViewer>(), 1);

        _world.Entity("HomeNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Home"
            });

        _world.Entity("LiteratureNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Literature"
            });

        _world.Entity("SpacedRepetitionNavigationViewItem")
            .ChildOf(navigationView)
            .Set(new NavigationViewItem()
            {
                Content = "Spaced Repetition"
            });

        Grid.SetColumn(navigationView.Get<NavigationView>(), 0);





        navigationView.Observe<Module.OnSelectionChanged>((Entity e) =>
        {

            navigationView.Children((Entity children) =>
            {

                //children.Remove<Ecs.ChildOf, Ecs.WildCard>();
            });

            var OnSelectionChanged = e.Get<Module.OnSelectionChanged>();

            var selectedItem = OnSelectionChanged.Args.SelectedItem as NavigationViewItem;
            if (selectedItem?.Content.ToString() == "Home")
            {
                homePage.ChildOf(navigationView);
            }
            else if (selectedItem?.Content.ToString() == "Literature")
            {
                Console.WriteLine("Selection Changed To Literature");
                navigationView.Get<NavigationView>().Content = literaturePage.Get<TextBlock>();

            }
            else if (selectedItem?.Content.ToString() == "Spaced Repetition")
            {
                navigationView.Get<NavigationView>().Content = spacedRepetitionPage.Get<Panel>();
            }
            else if (selectedItem?.Content.ToString() == "Settings")
            {
                Console.WriteLine("Selection Changed To Settings");
                navigationView.Get<NavigationView>().Content = settingPage.Get<TextBlock>();
            }
        });



    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _world.Lookup("MainWindow").Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
        this.AttachDevTools();

    }
}