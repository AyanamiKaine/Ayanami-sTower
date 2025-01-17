using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.UiComponents;

/// <summary>
/// Ui component
/// </summary>
public static class ComparePriority
{
    /// <summary>
    /// Creates A ComparePriority component
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="layout"></param>
    /// <param name="CreateButton"></param>
    /// <returns></returns>
    public static (Entity priorityGrid, Entity calculatedPriorityEntity) Create(NamedEntities entities, Entity layout, Entity CreateButton)
    {
        // We want that the user can only compare two times, no need to compare to all items
        var timesCompared = 0;


        var spacedRepetitionItems = entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>();

        //We are doing so the calucalted priority value is always reflected as the newest one.
        var calculatedPriorityEntity = entities.Create()
            .Set(500000000);

        int heighestPossiblePriority = 999999999;
        int smallestPossiblePriority = 0;

        SpacedRepetitionItem? currentItemToCompare = null;
        string? currentItemName;
        var rng = new Random();

        var priorityGrid = entities.Create()
            .Set(currentItemToCompare)
            .ChildOf(layout)
            .Set(new Grid())
            .SetColumnDefinitions("*,*")
            .SetRowDefinitions("*,*,*");

        var priorityTextblock = entities.Create()
            .ChildOf(priorityGrid)
            .Set(new TextBlock()
            {
                TextWrapping = Media.TextWrapping.Wrap
            })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetRow(0)
            .SetColumnSpan(2)
            .SetText("Is the new item more or less important than this one?");

        var lessPriorityButton = entities.Create()
            .ChildOf(priorityGrid)
            .Set(new Button())
            .SetContent("Less")
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Left)
            .SetMargin(20)
            .SetColumn(0)
            .SetRow(2);

        var morePriorityButton = entities.Create()
            .ChildOf(priorityGrid)
            .Set(new Button())
            .SetContent("More")
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Right)
            .SetMargin(20)
            .SetColumn(1)
            .SetRow(2);

        if (spacedRepetitionItems?.Count != 0 && spacedRepetitionItems is not null)
        {
            currentItemToCompare = spacedRepetitionItems.OrderBy(x => rng.Next()).First();
            currentItemName = currentItemToCompare.Name;
        }
        else
        {
            morePriorityButton.Get<Button>().IsEnabled = false;
            lessPriorityButton.Get<Button>().IsEnabled = false;
            currentItemToCompare = null;
            currentItemName = "No Items to compare to";
        }

        var itemToCompareToTextBlock = entities.Create()
            .ChildOf(priorityGrid)
            .Set(new TextBlock()
            {
                TextWrapping = Media.TextWrapping.Wrap,
                FontWeight = Media.FontWeight.Bold
            })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetMargin(20)
            .SetRow(1)
            .SetColumnSpan(2)
            .SetText(currentItemName!);

        spacedRepetitionItems!.CollectionChanged += ((sender, e) =>
        {
            if (spacedRepetitionItems?.Count != 0 && spacedRepetitionItems is not null)
            {
                var randomIndex = rng.Next(spacedRepetitionItems.Count);
                currentItemToCompare = spacedRepetitionItems[randomIndex];

                if (currentItemToCompare is not null)
                    currentItemName = currentItemToCompare.Name;
                else
                    currentItemName = "No Items to compare to";

                itemToCompareToTextBlock.SetText(currentItemName);
            }
            else
            {
                currentItemToCompare = null;
                currentItemName = "No Items to compare to";
                itemToCompareToTextBlock.SetText(currentItemName);
            }
        });

        lessPriorityButton.OnClick((_, _) =>
            {
                timesCompared++;

                calculatedPriorityEntity.Set<int>(currentItemToCompare!.Priority - rng.Next(50));

                heighestPossiblePriority = calculatedPriorityEntity.Get<int>();

                var itemsBetweenLowAndHighPriority = spacedRepetitionItems
                    .Where(x => x.Priority >= smallestPossiblePriority && x.Priority <= heighestPossiblePriority);

                currentItemToCompare = itemsBetweenLowAndHighPriority
                    .OrderBy(x => x.Priority > heighestPossiblePriority)
                    .Reverse()
                    .FirstOrDefault();

                if (currentItemToCompare is null || currentItemToCompare.Priority > calculatedPriorityEntity.Get<int>())
                {
                    if (timesCompared >= 3)
                    {
                        currentItemName = "Compared Enough Items";
                    }
                    else
                    {
                        currentItemName = "No more items to compare to";
                    }

                    lessPriorityButton.Get<Button>().IsEnabled = false;
                    morePriorityButton.Get<Button>().IsEnabled = false;
                }
                else
                {
                    currentItemName = currentItemToCompare.Name;
                }
                itemToCompareToTextBlock.SetText(currentItemName);
            });

        morePriorityButton.OnClick((_, _) =>
            {
                timesCompared++;

                if (currentItemToCompare is not null)
                    calculatedPriorityEntity.Set<int>(currentItemToCompare!.Priority + rng.Next(50));
                else
                    calculatedPriorityEntity.Set<int>(rng.Next(1000));

                smallestPossiblePriority = calculatedPriorityEntity.Get<int>();

                currentItemToCompare = spacedRepetitionItems
                    .Where(x => x.Priority >= smallestPossiblePriority && x.Priority <= heighestPossiblePriority)
                    .OrderBy(x => x.Priority)
                    .FirstOrDefault();

                if (currentItemToCompare is null || currentItemToCompare.Priority < calculatedPriorityEntity.Get<int>())
                {
                    if (timesCompared >= 3)
                    {
                        currentItemName = "Compared Enough Items";
                    }
                    else
                    {
                        currentItemName = "No more items to compare to";
                    }

                    lessPriorityButton.Get<Button>().IsEnabled = false;
                    morePriorityButton.Get<Button>().IsEnabled = false;
                }
                else
                {
                    currentItemName = currentItemToCompare.Name;
                }
                itemToCompareToTextBlock.SetText(currentItemName);
            });

        CreateButton.OnClick((_, _) =>
        {
            heighestPossiblePriority = 999999999;
            smallestPossiblePriority = 0;
            morePriorityButton.Get<Button>().IsEnabled = true;
            lessPriorityButton.Get<Button>().IsEnabled = true;
            timesCompared = 0;
        });

        return (priorityGrid, calculatedPriorityEntity);
    }
}