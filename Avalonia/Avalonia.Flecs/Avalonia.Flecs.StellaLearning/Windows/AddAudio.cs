using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.Util;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public static class AddAudio
{

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var addAudioWindow = entities.GetEntityCreateIfNotExist("AddAudioWindow")
            .Set(new Window())
            .SetWindowTitle("Add Audio File")
            .SetWidth(400)
            .SetHeight(400);


        var scrollViewer = entities.Create()
            .ChildOf(addAudioWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            addAudioWindow.CloseWindow();
        });

        addAudioWindow.OnClosing((s, e) =>
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

        return addAudioWindow;
    }

    private static Entity DefineWindowContents(NamedEntities entities)
    {
        var layout = entities.Create()
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

        var nameTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var questionTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Question");


        var filePickerButton = FilePickerButton(entities);

        var filePath = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("FilePath")
            .SetInnerRightContent(filePickerButton);

        filePickerButton.OnClick(async (e, args) => filePath.SetText(await FilePickerAsync(entities)));

        ObservableCollection<Tag> tags = [];


        var tagsTextBox = entities.Create()
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

        var tagsList = entities.Create()
            .ChildOf(layout)
            .Set(new ItemsControl())
            .Set(tags)
            .SetItemTemplate(DefineTagTemplate(entities))
            .SetItemsSource(tags);

        var createFileButton = entities.Create()
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
                    Question = questionTextBox.GetText(),
                    FilePath = filePath.GetText(),
                    SpacedRepetitionItemType = SpacedRepetitionItemType.Audio
                });

                nameTextBox.SetText("");
                questionTextBox.SetText("");
                filePath.SetText("");
                tags.Clear();
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
            Title = "Select an Audio File",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
            FileTypeFilter = new List<FilePickerFileType>
            {
                CustomFilePickerTypes.AudioFileType,
                FilePickerFileTypes.All // Always include "All files" as an option
            }
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