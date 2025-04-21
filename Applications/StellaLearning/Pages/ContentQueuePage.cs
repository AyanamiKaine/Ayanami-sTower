/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Layout;
using NLog;
using Avalonia.Flecs.Controls;

namespace AyanamisTower.StellaLearning.Pages;
/// <summary>
/// Content Queue Page
/// </summary>
public class ContentQueuePage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create the Content Queue Page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public ContentQueuePage(World world)
    {
        _root = world.UI<Grid>((grid) =>
        {
            grid
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");

            grid.Child<TextBox>((textBox) =>
            {
                textBox
                .SetColumn(0)
                .SetRow(0)
                .SetWatermark("Search Entries");
            });

            grid.Child<TextBlock>((textBlock) =>
            {
                textBlock
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetMargin(10, 0)
                .SetText("Total Items: 0")
                .SetRow(0)
                .SetColumn(1);
            });

            grid.Child<ComboBox>((comboBox) =>
            {
                List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];
                var myFlyout = new Flyout()
                {
                    Content = new TextBlock() { Text = "Hello World" },
                    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
                };
                comboBox
                    .SetColumn(2)
                    .SetPlaceholderText("Sort Items")
                    .SetItemsSource(sortItems)
                    .SetContextFlyout(myFlyout);
            });

            grid.Child<ScrollViewer>((scrollViewer) =>
            {
                scrollViewer.SetRow(1).SetColumnSpan(3);
                scrollViewer.Child<ListBox>((listBox) =>
                {
                    ObservableCollection<string> contentQueueItems = [];
                    listBox
                        .SetItemsSource(contentQueueItems)
                        .SetSelectionMode(SelectionMode.Multiple);

                });
            });
        }).Add<Page>().Entity;
    }

    /// <inheritdoc/>
    public void Attach(Entity entity)
    {
        _root.ChildOf(entity);
    }

    /// <inheritdoc/>
    public void Detach()
    {
        _root.Remove(Ecs.ChildOf);
    }
}
