using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public static class AddFile
{

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var addFileWindow = entities.GetEntityCreateIfNotExist("AddFileWindow")
            .Set(new Window())
            .SetWindowTitle("Add File")
            .SetWidth(400)
            .SetHeight(400);


        var scrollViewer = entities.GetEntityCreateIfNotExist("AddFileScrollViewer")
            .ChildOf(addFileWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            addFileWindow.CloseWindow();
        });

        addFileWindow.OnClosing((s, e) =>
        {
            // As long as the main window is visible dont 
            // close the window but hide it instead
            if (entities["MainWindow"].Get<Window>().IsVisible)
            {
                ((Window)s!).Hide();
                e.Cancel = true;
            }
        });

        DefineWindowContents(entities).ChildOf(scrollViewer);

        return addFileWindow;
    }

    private static Entity DefineWindowContents(NamedEntities entities)
    {
        var layout = entities.GetEntityCreateIfNotExist("AddFileLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

        var nameTextBox = entities.GetEntityCreateIfNotExist("nameTextBox")
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var questionTextBox = entities.GetEntityCreateIfNotExist("questionTextBox")
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Question");

        var filePickerButton = FilePickerButton(entities);

        var filePath = entities.GetEntityCreateIfNotExist("filePathTextBox")
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("FilePath")
            .SetInnerRightContent(filePickerButton);

        filePickerButton.OnClick(async (e, args) => filePath.SetText(await FilePickerAsync(entities)));

        ObservableCollection<Tag> tags = [];

        var tagsTextBox = entities.GetEntityCreateIfNotExist("tagsTextBox")
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Tags");

        tagsTextBox.OnKeyDown((sender, args) =>
        {
            if (args.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(tagsTextBox.GetText()))
                {
                    return;
                }

                tags.Add(new(tagsTextBox.GetText()));
                tagsTextBox.SetText("");
            }
        });

        var tagsList = entities.GetEntityCreateIfNotExist("tagsList")
            .ChildOf(layout)
            .Set(new ItemsControl())
            .Set(tags)
            .SetItemTemplate(DefineTagTemplate(entities))
            .SetItemsSource(tags);

        /*
        Determine the correct priority

        How are we gonna do this?
        Priority will be determined based on questions like is the priority higher or lower of an already created item?
        
        */
        var spacedRepetitionItems = entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>();

        int calculatedPriority = 500000000;
        int heighestPossiblePriority = 999999999;
        int smallestPossiblePriority = 0;

        SpacedRepetitionItem? currentItemToCompare;
        string? currentItemName;
        var rng = new Random();

        var priorityGrid = entities.Create()
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
                currentItemToCompare = spacedRepetitionItems
                    .Where(x => x.Priority >= smallestPossiblePriority && x.Priority <= heighestPossiblePriority)
                    .OrderBy(x => x.Priority)
                    .FirstOrDefault();

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
                calculatedPriority = currentItemToCompare!.Priority - 1;

                heighestPossiblePriority = calculatedPriority;

                var itemsBetweenLowAndHighPriority = spacedRepetitionItems
                    .Where(x => x.Priority >= smallestPossiblePriority && x.Priority <= heighestPossiblePriority);

                currentItemToCompare = itemsBetweenLowAndHighPriority
                    .OrderBy(x => x.Priority > heighestPossiblePriority)
                    .Reverse()
                    .FirstOrDefault();

                if (currentItemToCompare is null || currentItemToCompare.Priority > calculatedPriority)
                {
                    currentItemName = "No more items to compare to";
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
                calculatedPriority = currentItemToCompare!.Priority + 1;

                smallestPossiblePriority = calculatedPriority;

                currentItemToCompare = spacedRepetitionItems
                    .Where(x => x.Priority >= smallestPossiblePriority && x.Priority <= heighestPossiblePriority)
                    .OrderBy(x => x.Priority)
                    .FirstOrDefault();

                if (currentItemToCompare is null || currentItemToCompare.Priority < calculatedPriority)
                {
                    currentItemName = "No more items to compare to";
                    morePriorityButton.Get<Button>().IsEnabled = false;
                    lessPriorityButton.Get<Button>().IsEnabled = false;
                }
                else
                {
                    currentItemName = currentItemToCompare.Name;
                }
                itemToCompareToTextBlock.SetText(currentItemName);
            });

        var createFileButton = entities.GetEntityCreateIfNotExist("createFileButton")
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Create Item")
            .OnClick((sender, args) =>
            {
                if (string.IsNullOrEmpty(nameTextBox.GetText()) || string.IsNullOrEmpty(filePath.GetText()))
                {
                    nameTextBox.SetWatermark("Name is required");
                    filePath.SetWatermark("FilePath is required");
                    return;
                }

                entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionFile()
                {
                    Name = nameTextBox.GetText(),
                    Priority = calculatedPriority,
                    Question = questionTextBox.GetText(),
                    FilePath = filePath.GetText(),
                    SpacedRepetitionItemType = SpacedRepetitionItemType.File
                });
                morePriorityButton.Get<Button>().IsEnabled = true;
                lessPriorityButton.Get<Button>().IsEnabled = true;

                smallestPossiblePriority = 0;
                heighestPossiblePriority = 999999999;
                nameTextBox.SetText("");
                questionTextBox.SetText("");
                filePath.SetText("");
                tags.Clear();
                calculatedPriority = 5000000;
            });

        return layout;
    }

    private static Entity FilePickerButton(NamedEntities entities)
    {
        var browseForFileButton = entities.GetEntityCreateIfNotExist("BrowseForFileButton")
            .Set(new Button());

        var browseForFileButtonContent = entities.GetEntityCreateIfNotExist("BrowseForFileButtonContent")
            .ChildOf(browseForFileButton)
            .Set(new TextBlock())
            .SetText("Browse");

        return browseForFileButton;
    }

    private static async Task<string> FilePickerAsync(NamedEntities entities)
    {
        // Create and configure the file picker options
        var options = new FilePickerOpenOptions
        {
            Title = "Select the obsidian executable",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
        };

        // Create an OpenFileDialog instance
        IReadOnlyList<IStorageFile> result = await entities["MainWindow"].Get<Window>().StorageProvider.OpenFilePickerAsync(options);

        if (result != null && result.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            return file.TryGetLocalPath()!;
        }
        return string.Empty;
    }

    private static FuncDataTemplate<Tag> DefineTagTemplate(NamedEntities entities)
    {
        return new FuncDataTemplate<Tag>((tag, _) =>
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 5
            };

            var nameText = new TextBlock()
            {
                Text = tag.Name
            };

            var removeButton = new Button()
            {
                Content = "X"
            };

            removeButton.Click += ((sender, args) =>
            {
                entities["tagsList"].Get<ObservableCollection<Tag>>().Remove(tag);
            });
            /*
            removeButton.OnClick((sender, args) =>
            {
                entities["Tags"].Get<ObservableCollection<Tag>>().Remove(tag);
            });
            */

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(removeButton);
            return stackPanel;
        });
    }
}