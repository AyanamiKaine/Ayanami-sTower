/*
Stella Learning is a modern learning app.
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables; // For CompositeDisposable
using System.Text.Json; // For saving/loading (will need converters)
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Flecs.Controls; // For UIBuilderExtensions
using Avalonia.Input; // For DragEventArgs, KeyEventArgs
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage; // For FilePicker
using Avalonia.Threading; // For Dispatcher
using AyanamisTower.StellaLearning.Converters;
using AyanamisTower.StellaLearning.Data; // Where LiteratureSourceItem etc. reside
using AyanamisTower.StellaLearning.Extensions; // Assuming you use NLog like in SpacedRepetitionPage
using AyanamisTower.StellaLearning.Util; // For MessageDialog (assuming it's here)
using AyanamisTower.StellaLearning.Windows;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module; // If Page tag is defined here

namespace AyanamisTower.StellaLearning.Pages;

/// <summary>
/// Represents the UI page for managing literature sources.
/// </summary>
public class LiteraturePage : IUIComponent, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly AttachedProperty<Entity> ContainerEntityProperty =
        AvaloniaProperty.RegisterAttached<LiteraturePage, Control, Entity>("ContainerEntity");
    private readonly World _world;
    private Entity _root;

    /// <inheritdoc/>
    public Entity Root => _root;

    private bool _isDisposed = false;
    private readonly CompositeDisposable _disposables = [];

    private readonly Dictionary<Guid, PropertyChangedEventHandler> _itemPropertyChangedHandlers =
    [];

    // --- Data Collections ---
    // The master list of all literature items
    private readonly ObservableCollection<LiteratureSourceItem> _baseLiteratureItems;

    // The currently displayed list (after filtering/sorting) - ListBox ItemsSource will point to a filtered version
    // We don't necessarily need a separate collection field if ApplyFilterAndSort creates a new one each time.

    // --- Control References ---
    private UIBuilder<ListBox>? _literatureListBuilder;
    private UIBuilder<TextBox>? _searchTextBoxBuilder;
    private UIBuilder<TextBlock>? _itemCountTextBlockBuilder;

    // --- Constants ---
    private const string LITERATURE_FOLDER_NAME = "literature"; // Folder to store local files
    private const string LITERATURE_SAVE_FILE = "literature_items.json"; // Save file name

    /// <summary>
    /// Initializes a new instance of the LiteraturePage class.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    public LiteraturePage(World world)
    {
        _world = world;

        // Ensure literature directory exists
        EnsureLiteratureDirectory();
        _world.Set(LoadLiteratureItemsFromDisk());
        // Load existing data
        _baseLiteratureItems = _world.Get<ObservableCollection<LiteratureSourceItem>>();
        _baseLiteratureItems.RemoveDuplicateFilePaths();

        SubscribeToAllItemChanges(_baseLiteratureItems);

        // Subscribe to collection changes for saving and UI updates
        _baseLiteratureItems.CollectionChanged += OnBaseCollectionChanged;
        _disposables.Add(
            Disposable.Create(
                () => _baseLiteratureItems.CollectionChanged -= OnBaseCollectionChanged
            )
        );

        // Build the UI
        _root = _world
            .UI<Grid>(BuildLiteraturePageUI)
            .Add<Page>() // Add the Page tag
            .Entity;

        // Initial filter/sort application
        Dispatcher.UIThread.Post(
            () => ApplyFilterAndSort(string.Empty),
            DispatcherPriority.Background
        );

        // Setup auto-save timer (similar to SpacedRepetitionPage)
        // var autoSaveTimer = CreateAutoSaveTimer(_baseLiteratureItems);
        // var timerEntity = world.Entity().Set(autoSaveTimer);
        // _disposables.Add(Disposable.Create(() => timerEntity.Destruct()));

        // Save on close
        App.GetMainWindow().Closing += async (_, _) =>
        {
            _baseLiteratureItems.RemoveDuplicateFilePaths();
            await SaveLiteratureItemsToDiskAsync(_baseLiteratureItems);
        };
    }

    /// <summary>
    /// Ensures the directory for storing local literature files exists.
    /// </summary>
    private static void EnsureLiteratureDirectory()
    {
        try
        {
            string literaturePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                LITERATURE_FOLDER_NAME
            );
            Directory.CreateDirectory(literaturePath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error creating literature directory: {LITERATURE_FOLDER_NAME}");
            MessageDialog.ShowErrorDialog($"Error creating literature directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds the UI structure for the LiteraturePage using UIBuilder.
    /// </summary>
    private void BuildLiteraturePageUI(UIBuilder<Grid> grid)
    {
        grid.SetRowDefinitions("Auto, *, Auto") // Row 0: Controls, Row 1: List, Row 2: Actions
            .SetColumnDefinitions("*, Auto, Auto"); // Col 0: Search, Col 1: Count, Col 2: Add/Sort

        // --- Row 0: Search, Count, Add/Sort ---
        grid.Child<TextBox>(textBox =>
        {
            _searchTextBoxBuilder = textBox;
            textBox
                .SetColumn(0)
                .SetWatermark("Search by Name, Title, Author, Summary, Tags...")
                .SetMargin(5)
                .OnTextChanged(
                    (sender, args) =>
                    {
                        ApplyFilterAndSort(_searchTextBoxBuilder?.GetText() ?? string.Empty);
                    }
                );
            textBox.AttachToolTip(
                _world.UI<ToolTip>(tooltip =>
                    tooltip.Child<TextBlock>(tb =>
                        tb.SetText("Search across item names, titles, authors, and tags.")
                    )
                )
            );
        });

        grid.Child<TextBlock>(textBlock =>
        {
            _itemCountTextBlockBuilder = textBlock;
            textBlock
                .SetColumn(1)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetMargin(5)
                .SetText($"Items: {_baseLiteratureItems.Count}"); // Initial count
        });

        grid.Child<Button>(button => // Placeholder for Add/Sort
        {
            button
                .SetColumn(2)
                .SetMargin(5)
                .SetText("Add Source...")
                .OnClick(async (s, e) => await AddSourceAsync()); // Implement AddSourceAsync
            button.AttachToolTip(
                _world.UI<ToolTip>(tooltip =>
                    tooltip.Child<TextBlock>(tb =>
                        tb.SetText(
                            "Add a new literature source (File or URL). You can also drag & drop them onto the list. Try it! This copies the file to the local literature folder."
                        )
                    )
                )
            );

            // TODO: Add MenuFlyout for choosing File/URL if desired
        });

        // --- Row 1: Literature List ---
        grid.Child<ScrollViewer>(scrollViewer =>
        {
            scrollViewer
                .SetRow(1)
                .SetColumnSpan(3) // Span all columns
                .SetMargin(5, 0, 5, 0);

            scrollViewer.Child<ListBox>(listBox =>
            {
                _literatureListBuilder = listBox;

                // Context Menu Setup
                var contextFlyout = _world.UI<MenuFlyout>(menuFlyout =>
                {
                    menuFlyout.OnOpened(
                        (sender, e) =>
                        {
                            if (!listBox.HasItemSelected())
                            {
                                menuFlyout.Hide();
                            }
                        }
                    );

                    menuFlyout.Child<MenuItem>(item =>
                        item.SetHeader("Open")
                            .OnClick(
                                (_, _) =>
                                {
                                    var selectedLiteratureItem =
                                        listBox.GetSelectedItem<LiteratureSourceItem>();
                                    if (selectedLiteratureItem is LocalFileSourceItem localFile)
                                    {
                                        if (Path.GetExtension(localFile.FilePath) == ".md")
                                        {
                                            FileOpener.OpenMarkdownFileWithObsidian(
                                                localFile.FilePath,
                                                _world.Get<Settings>().ObsidianPath
                                            );
                                        }
                                        FileOpener.OpenFileWithDefaultProgram(localFile.FilePath);
                                    }
                                    else if (selectedLiteratureItem is WebSourceItem webSourceItem)
                                    {
                                        SmartUrlOpener.OpenUrlIntelligently(webSourceItem.Url);
                                    }
                                    else
                                    {
                                        throw new NotImplementedException(
                                            "Opening this source type is not implemented yet."
                                        );
                                    }
                                }
                            )
                            .AttachToolTip(
                                _world.UI<ToolTip>(
                                    (tooltip) =>
                                    {
                                        tooltip.Child<TextBlock>(
                                            (textBlock) =>
                                            {
                                                textBlock.SetText(
                                                    "You can also double click to open it."
                                                );
                                            }
                                        );
                                    }
                                ),
                                false
                            )
                    );

                    menuFlyout.Child<MenuItem>(item =>
                        item.SetHeader("Edit Details...")
                            .OnClick(
                                (_, _) =>
                                {
                                    var selectedLiteratureItem =
                                        listBox.GetSelectedItem<LiteratureSourceItem>();
                                    new EditLiteratureSource(_world, selectedLiteratureItem);
                                }
                            )
                    );

                    menuFlyout.Child<MenuItem>(item =>
                        item.SetHeader("Generate Metadata")
                            .OnClick(
                                async (_, _) =>
                                {
                                    var selectedLiteratureItem =
                                        listBox.GetSelectedItem<LiteratureSourceItem>();

                                    if (selectedLiteratureItem is LocalFileSourceItem localFile)
                                    {
                                        ContentMetadata? metaData = null;

                                        if (Path.GetExtension(localFile.FilePath) == ".pdf")
                                        {
                                            try
                                            {
                                                PdfMetadata info =
                                                    PdfSharpMetadataExtractor.ExtractMetadata(
                                                        localFile.FilePath
                                                    );
                                                localFile.PageCount = info.PageCount;
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Info($"Exception was thrown {ex}");
                                            }

                                            if (localFile.PageCount >= 1000)
                                            {
                                                MessageDialog.ShowWarningDialog(
                                                    $"Only pdfs smaller than 1000 pages can be used to generate metadata via a LLM"
                                                );
                                            }
                                            else
                                            {
                                                // Here we are passing a pdf that is smaller than 1000 pages
                                                metaData =
                                                    await LargeLanguageManager.Instance.GenerateMetaDataBasedOnFile(
                                                        localFile.FilePath
                                                    );
                                            }
                                        }
                                        else
                                        {
                                            metaData =
                                                await LargeLanguageManager.Instance.GenerateMetaDataBasedOnFile(
                                                    localFile.FilePath
                                                );
                                        }

                                        localFile.SourceType = LiteratureSourceType.LocalFile;
                                        localFile.Author = metaData?.Author ?? "";
                                        localFile.Tags = metaData?.Tags ?? [];
                                        localFile.Summary = metaData?.Summary ?? "";
                                    }
                                }
                            )
                    );

                    menuFlyout.Child<Separator>((_) => { });
                    menuFlyout.Child<MenuItem>(item =>
                        item.SetHeader("Remove")
                            .OnClick(
                                async (sender, e) =>
                                {
                                    if (listBox == null)
                                        return;

                                    var itemToRemove =
                                        listBox.GetSelectedItem<LiteratureSourceItem>();
                                    UIBuilder<ToggleSwitch>? toogleSwitch = null;
                                    var stack = _world.UI<StackPanel>(
                                        (stack) =>
                                        {
                                            stack.Child<TextBlock>(
                                                (text) =>
                                                {
                                                    text.SetText(
                                                            $"""
                                                            Do you want to remove: "{itemToRemove.Title}" from the list?.
                                                            """
                                                        )
                                                        .SetTextWrapping(TextWrapping.Wrap);
                                                }
                                            );
                                            if (itemToRemove is LocalFileSourceItem file)
                                            {
                                                stack.Child<DockPanel>(
                                                    (dockPanel) =>
                                                    {
                                                        dockPanel.Child<TextBlock>(
                                                            (textBlock) =>
                                                            {
                                                                textBlock
                                                                    .SetVerticalAlignment(
                                                                        VerticalAlignment.Center
                                                                    )
                                                                    .SetText(
                                                                        "Delete Underlying File?"
                                                                    )
                                                                    .SetTextWrapping(
                                                                        TextWrapping.Wrap
                                                                    );
                                                            }
                                                        );

                                                        dockPanel.Child<ToggleSwitch>(
                                                            (toggleSwitch) =>
                                                            {
                                                                toogleSwitch = toggleSwitch
                                                                    .SetDock(Dock.Right)
                                                                    .SetHorizontalAlignment(
                                                                        HorizontalAlignment.Right
                                                                    );

                                                                toggleSwitch.With(
                                                                    (toggleSwitch) =>
                                                                    {
                                                                        // toggleSwitch.IsChecked = _world.Get<Settings>().EnableNotifications;
                                                                    }
                                                                );

                                                                toggleSwitch.OnIsCheckedChanged(
                                                                    (sender, args) =>
                                                                    {
                                                                        //((ToggleSwitch)sender!).IsChecked;
                                                                    }
                                                                );
                                                            }
                                                        );

                                                        dockPanel.AttachToolTip(
                                                            _world.UI<ToolTip>(
                                                                (toolTip) =>
                                                                {
                                                                    toolTip.Child<TextBlock>(
                                                                        (textBlock) =>
                                                                        {
                                                                            textBlock.SetText(
                                                                                """
                                                                                When enabled the associated file will also be deleted. If you
                                                                                just want to remove the item from the list leave it unchecked! 
                                                                                """
                                                                            );
                                                                        }
                                                                    );
                                                                }
                                                            )
                                                        );
                                                    }
                                                );
                                            }
                                        }
                                    );

                                    var cd = new ContentDialog()
                                    {
                                        Title = "Attention",
                                        Content = stack.Get<Control>(),
                                        PrimaryButtonText = "Yes",
                                        SecondaryButtonText = "No",
                                        DefaultButton = ContentDialogButton.Secondary,
                                        IsSecondaryButtonEnabled = true,
                                    };

                                    var result = await cd.ShowAsync();
                                    if (result == ContentDialogResult.Secondary)
                                    {
                                        return;
                                    }

                                    if (
                                        itemToRemove != null
                                        && itemToRemove is LocalFileSourceItem localFile
                                    )
                                    {
                                        string filePath = localFile.FilePath;
                                        string displayName = localFile.Title; // For use in messages

                                        // Basic validation
                                        if (string.IsNullOrEmpty(filePath))
                                        {
                                            MessageDialog.ShowErrorDialog(
                                                $"Error: File path is missing for item '{displayName}'. Cannot delete."
                                            );
                                            return;
                                        }

                                        // --- RECOMMENDED: Insert Confirmation Dialog Here ---
                                        // Example (requires a dialog implementation):
                                        // var dialogService = GetSomeDialogService(); // Get your dialog service instance
                                        // bool confirmed = await dialogService.ShowConfirmationAsync(
                                        //    "Confirm Deletion",
                                        //    $"Are you sure you want to permanently delete the reference file '{displayName}'?\n\nThis action cannot be undone.");
                                        //
                                        // if (!confirmed)
                                        // {
                                        //     Console.WriteLine("Deletion cancelled by user.");
                                        //     return; // Stop if user cancels
                                        // }
                                        // --- End Confirmation Dialog Placeholder ---

                                        try
                                        {
                                            if (toogleSwitch?.IsChecked() ?? false)
                                                File.Delete(filePath);

                                            _baseLiteratureItems.Remove(itemToRemove);
                                        }
                                        catch (IOException ex) // Catch specific IO exceptions
                                        {
                                            MessageDialog.ShowErrorDialog(ex.Message);
                                        }
                                        catch (UnauthorizedAccessException authEx) // Catch permissions errors
                                        {
                                            MessageDialog.ShowErrorDialog(authEx.Message);
                                        }
                                        catch (Exception ex) // Catch any other unexpected errors
                                        {
                                            MessageDialog.ShowErrorDialog(
                                                $"An unexpected error occurred while deleting file '{filePath}' for item '{displayName}': {ex.Message}"
                                            );
                                        }
                                    }

                                    if (itemToRemove is WebSourceItem webSourceItem)
                                    {
                                        _baseLiteratureItems.Remove(itemToRemove);
                                    }
                                    // To Clean up the ui, avoid memory leak
                                    stack.Entity.Destruct();
                                }
                            )
                    ); // Implement RemoveSelectedItem
                });

                listBox
                    .SetItemsSource(_baseLiteratureItems) // Bind to the base list initially
                    .SetItemTemplate(CreateLiteratureItemTemplate())
                    .SetSelectionMode(SelectionMode.Single)
                    .SetContextFlyout(contextFlyout)
                    .AllowDrop() // Enable dropping files
                    .OnDragOver(HandleLiteratureListDragOver) // Handle hover effect
                    .OnDrop(HandleLiteratureListDropAsync)
                    .OnDoubleTapped(
                        (sender, e) =>
                        {
                            if (sender is ListBox listBox)
                            {
                                var selectedLiteratureItem =
                                    listBox.SelectedItem as LiteratureSourceItem;
                                if (selectedLiteratureItem is LocalFileSourceItem localFile)
                                {
                                    if (Path.GetExtension(localFile.FilePath) == ".md")
                                    {
                                        FileOpener.OpenMarkdownFileWithObsidian(
                                            localFile.FilePath,
                                            _world.Get<Settings>().ObsidianPath
                                        );
                                    }
                                    else
                                    {
                                        FileOpener.OpenFileWithDefaultProgram(localFile.FilePath);
                                    }
                                }
                                else if (selectedLiteratureItem is WebSourceItem webSourceItem)
                                {
                                    SmartUrlOpener.OpenUrlIntelligently(webSourceItem.Url);
                                }
                                e.Handled = true;
                            }
                        }
                    );
            });
        });
    }

    /// <summary>
    /// Creates the DataTemplate for displaying LiteratureSourceItem in the ListBox.
    /// </summary>
    private FuncDataTemplate<LiteratureSourceItem> CreateLiteratureItemTemplate()
    {
        /*
        TODO: THIS LEAKS MEMORY ALL OVER THE PLACE BECAUSE THE BINDINGS ARE NOT CORRECTLY REMOVED FROM THE
        AVALONIA OBJECTS SO THEY WILL ALWAYS BE REFRENCED AND NEVER GARBAGED COLLECTED, THIS NEEDS TO BE FIXED
        ASAP.
        */

        return _world.CreateTemplate<LiteratureSourceItem, Grid>(
            (grid, item) =>
            {
                if (item is null)
                    return;

                /*
                When the underlying template ui becomes detached from the list box we also
                destroy the related entities.
                */

                grid.SetColumnDefinitions("Auto, *, Auto") // Icon(Type), Main Info, Tags
                    .SetRowDefinitions("Auto, Auto") // Row 0: Name/Title, Row 1: Author/Year + Tags
                    .SetMargin(5);

                // Row 0, Col 0: Icon based on SourceType (Placeholder)
                grid.Child<TextBlock>(icon =>
                {
                    // Add Tooltip for icon maybe?
                    icon.AttachToolTip(
                        _world.UI<ToolTip>(tooltip =>
                            tooltip.Child<TextBlock>(tb => tb.SetText(item.SourceType.ToString()!))
                        ),
                        true
                    );
                });

                // Row 0, Col 1: Name / Title
                grid.Child<TextBlock>(nameTitle =>
                {
                    nameTitle
                        .SetColumn(1)
                        .SetRow(0)
                        .SetColumnSpan(2)
                        .SetFontWeight(FontWeight.Bold)
                        .SetTextWrapping(TextWrapping.NoWrap)
                        .SetTextTrimming(TextTrimming.CharacterEllipsis)
                        .SetBinding(
                            TextBlock.TextProperty,
                            new Binding(nameof(LiteratureSourceItem.Title)) // Bind to Title primarily
                            {
                                // Fallback to Name if Title is null/empty
                                FallbackValue = item.Name, // Show Name if Title is unset
                                TargetNullValue =
                                    item.Name // Show Name if Title becomes null
                                ,
                            }
                        );
                    // Tooltip showing full Title or Name
                    nameTitle.AttachToolTip(
                        _world.UI<ToolTip>(tooltip =>
                        {
                            tooltip.Child<StackPanel>(
                                (stackPanel) =>
                                {
                                    stackPanel.SetSpacing(10);

                                    stackPanel.Child<TextBlock>(tb =>
                                        tb.SetBinding(
                                            TextBlock.TextProperty,
                                            new Binding(nameof(LiteratureSourceItem.Title))
                                            {
                                                FallbackValue = item.Name,
                                                TargetNullValue = item.Name,
                                            }
                                        )
                                    );

                                    stackPanel.Child<TextBlock>(
                                        (tb) =>
                                        {
                                            tb.SetFontWeight(FontWeight.Normal)
                                                .SetBinding(
                                                    TextBlock.TextProperty,
                                                    new Binding(
                                                        nameof(LiteratureSourceItem.Summary)
                                                    )
                                                );
                                        }
                                    );

                                    stackPanel.Child<ItemsControl>(tagsList =>
                                    {
                                        tagsList
                                            .SetColumn(2)
                                            .SetRow(1)
                                            .SetHorizontalAlignment(HorizontalAlignment.Right)
                                            .SetItemsSource(item.Tags) // Bind to the item's Tags collection
                                                                       //.SetItemsPanel(new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = double.NaN })) // Use WrapPanel
                                            .SetItemTemplate(
                                                _world.CreateTemplate<string, Border>(
                                                    (border, tagText) => // Simple tag template
                                                    {
                                                        border
                                                            .SetCornerRadius(3)
                                                            .SetPadding(4, 1)
                                                            .SetMargin(2)
                                                            .Child<StackPanel>(stackPanel =>
                                                            {
                                                                stackPanel
                                                                    .SetOrientation(
                                                                        Orientation.Horizontal
                                                                    )
                                                                    .SetSpacing(5)
                                                                    .SetVerticalAlignment(
                                                                        VerticalAlignment.Center
                                                                    );

                                                                stackPanel.Child<TextBlock>(
                                                                    textBlock =>
                                                                    {
                                                                        textBlock
                                                                            .SetTextWrapping(
                                                                                TextWrapping.NoWrap
                                                                            )
                                                                            .SetTextTrimming(
                                                                                TextTrimming.CharacterEllipsis
                                                                            )
                                                                            .SetText(tagText)
                                                                            .SetVerticalAlignment(
                                                                                VerticalAlignment.Center
                                                                            );
                                                                    }
                                                                );
                                                            });

                                                        if (_world.Get<Settings>().IsDarkMode)
                                                        {
                                                            // Dark Gray
                                                            border.SetBackground(new SolidColorBrush(Color.FromRgb(51, 50, 48)));
                                                        }
                                                        else
                                                        {
                                                            border.SetBackground(Brushes.LightGray);
                                                        }
                                                        /*
                                                        There is no need to clean up this template because the when the ItemTemplate
                                                        gets cleaned all child templates get cleaned too, we want to avoid an double free.
            
                                                        THIS IS STILL BUGGED AND RESULTS IN A MEMORY LEAK, THE SECOND NESTED TEMPLATE NEVER GETS DESTROYED AND I DONT KNOW WHY!
            
                                                        I tracked down the problem to flecs. It seems to occur when we delete an entity and
                                                        something in anohter thread? runs and we get entity_index.c: 72: assert: r->dense < index->alive_count INVALID_PARAMETER.
            
                                                        ###################### THIS PROBLEM IS FIXED !!!! ##########################
                                                        Using the newest version of flecs.net 4.0.4 fixed the problem, using 4.0.3 results again in the crash.
                                                        */
                                                    }
                                                )
                                            )
                                            .With(ic =>
                                                ic.ItemsPanel = new FuncTemplate<Panel>(
                                                    () =>
                                                        new WrapPanel
                                                        {
                                                            Orientation = Orientation.Horizontal,
                                                        }
                                                )!
                                            );
                                    });
                                }
                            );
                        }),
                        true
                    );
                });

                // Row 1, Col 1: Author / Year
                grid.Child<TextBlock>(authorYear =>
                {
                    authorYear
                        .SetColumn(1)
                        .SetRow(1)
                        .SetFontSize(11)
                        .SetForeground(Brushes.Gray)
                        .SetTextTrimming(TextTrimming.CharacterEllipsis);

                    // Combine Author and Year - requires a MultiBinding or custom converter,
                    // or just display Author for simplicity for now.
                    authorYear.SetBinding(
                        TextBlock.TextProperty,
                        new Binding(nameof(LiteratureSourceItem.Author))
                        {
                            TargetNullValue = "No Author", // Placeholder if author is null
                            FallbackValue = "No Author",
                        }
                    );
                    // TODO: Improve this display to include year, possibly using a converter.
                    authorYear.AttachToolTip(
                        _world.UI<ToolTip>(tooltip =>
                            tooltip.Child<TextBlock>(tb =>
                                tb.SetBinding(
                                    TextBlock.TextProperty,
                                    new Binding(nameof(LiteratureSourceItem.Author))
                                    {
                                        TargetNullValue = "No Author",
                                        FallbackValue = "No Author",
                                    }
                                )
                            )
                        ),
                        true
                    );
                });

                // Row 1, Col 2: Tags (using ItemsControl or similar)
                grid.Child<ItemsControl>(tagsList =>
                {
                    tagsList
                        .SetColumn(2)
                        .SetRow(1)
                        .SetOpacity(1)
                        .SetHorizontalAlignment(HorizontalAlignment.Right)
                        .SetItemsSource(item.Tags) // Bind to the item's Tags collection
                                                   //.SetItemsPanel(new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = double.NaN })) // Use WrapPanel
                        .SetItemTemplate(
                            _world.CreateTemplate<string, Border>(
                                (border, tagText) => // Simple tag template
                                {
                                    border
                                        .SetBackground(Brushes.LightGray)
                                        .SetCornerRadius(3)
                                        .SetPadding(4, 1)
                                        .SetMargin(2, 0)
                                        .Child<StackPanel>(stackPanel =>
                                        {
                                            stackPanel
                                                .SetOrientation(Orientation.Horizontal)
                                                .SetSpacing(5)
                                                .SetVerticalAlignment(VerticalAlignment.Center);

                                            stackPanel.Child<TextBlock>(textBlock =>
                                            {
                                                textBlock
                                                    .SetText(tagText)
                                                    .SetVerticalAlignment(VerticalAlignment.Center);
                                            });
                                        });

                                    if (_world.Get<Settings>().IsDarkMode)
                                    {
                                        // Dark Gray
                                        border.SetBackground(new SolidColorBrush(Color.FromRgb(51, 50, 48)));
                                    }
                                    else
                                    {
                                        border.SetBackground(Brushes.LightGray);
                                    }

                                    /*
                                    There is no need to clean up this template because the when the ItemTemplate
                                    gets cleaned all child templates get cleaned too, we want to avoid an double free.
        
                                    THIS IS STILL BUGGED AND RESULTS IN A MEMORY LEAK, THE SECOND NESTED TEMPLATE NEVER GETS DESTROYED AND I DONT KNOW WHY!
        
                                    I tracked down the problem to flecs. It seems to occur when we delete an entity and
                                    something in anohter thread? runs and we get entity_index.c: 72: assert: r->dense < index->alive_count INVALID_PARAMETER.
        
                                    ###################### THIS PROBLEM IS FIXED !!!! ##########################
                                    Using the newest version of flecs.net 4.0.4 fixed the problem, using 4.0.3 results again in the crash.
                                    */
                                }
                            )
                        )
                        .With(ic =>
                            ic.ItemsPanel = new FuncTemplate<Panel>(
                                () => new WrapPanel { Orientation = Orientation.Horizontal }
                            )!
                        );
                });
            }
        );
    }

    /// <summary>
    /// Gets a simple string icon representation for the source type.
    /// </summary>
    private string GetIconForSourceType(LiteratureSourceType type)
    {
        // Replace with actual icons (e.g., from Fluent Icons) later
        return type switch
        {
            LiteratureSourceType.Book => "\uE736", // Book icon (Fluent System Icons)
            LiteratureSourceType.JournalArticle => "\uE8A5", // Document icon
            LiteratureSourceType.Website => "\uE774", // Globe icon
            LiteratureSourceType.LocalFile => "\uE7C3", // File icon
            LiteratureSourceType.ConferencePaper => "\uE8A5", // Document icon
            LiteratureSourceType.Report => "\uE7F6", // Report icon
            LiteratureSourceType.Thesis => "\uE7BC", // Education icon
            _ => "\uE783", // Question mark icon
        };
    }

    // --- Event Handlers ---

    private void SubscribeToAllItemChanges(IEnumerable<LiteratureSourceItem>? items)
    {
        if (items == null)
            return;
        foreach (var item in items)
        {
            SubscribeToItemChanges(item);
        }
    }

    private void SubscribeToItemChanges(LiteratureSourceItem item)
    {
        if (item == null || _itemPropertyChangedHandlers.ContainsKey(item.Uid))
        {
            return; // Avoid double subscription or null refs
        }

        PropertyChangedEventHandler handler = HandleItemPropertyChanged;
        item.PropertyChanged += handler;
        _itemPropertyChangedHandlers[item.Uid] = handler; // Store the specific handler instance
    }

    private void HandleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isDisposed)
            return; // Don't handle if page is disposed
        if (sender is not LiteratureSourceItem changedItem)
            return;

        // Check if the changed property is relevant for filtering or sorting
        bool needsRefresh = e.PropertyName switch
        {
            nameof(LiteratureSourceItem.Name) => true,
            nameof(LiteratureSourceItem.Title) => true,
            nameof(LiteratureSourceItem.Author) => true,
            nameof(LiteratureSourceItem.Tags) => true,
            nameof(LiteratureSourceItem.SourceType) => true,
            _ => false,
        };

        if (needsRefresh)
        {
            Logger.Trace(
                $"Relevant property '{e.PropertyName}' changed for item '{changedItem.Name}', triggering list refresh."
            );
            // Refresh the list on the UI thread. Use Post for better responsiveness if updates are frequent.
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_isDisposed)
                        return; // Double check dispose state
                    string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;
                    ApplyFilterAndSort(searchText);
                },
                DispatcherPriority.Background
            ); // Lower priority for UI updates
        }
    }

    /// <summary>
    /// Handles the DragOver event for the literature ListBox.
    /// Allows dropping only if files are being dragged.
    /// </summary>
    private void HandleLiteratureListDragOver(object? sender, DragEventArgs e)
    {
        // Allow copy effect if data contains files.
        bool allowDrop = false;
        DragDropEffects proposedEffect = DragDropEffects.None;

        // Check 1: Are files being dragged?
        bool containsFiles = e.Data.Contains(DataFormats.Files);
        if (containsFiles)
        {
            allowDrop = true;
            proposedEffect = DragDropEffects.Copy; // Files are usually copied
        }

        // Check 2: Is text being dragged? If so, is it a valid web URL?
        bool containsTextUrl = false;
        if (e.Data.Contains(DataFormats.Text))
        {
            var potentialText = e.Data.Get(DataFormats.Text) as string;
            // Use the helper method to check if the text is a valid web URL
            if (IsValidWebUrl(potentialText, out _)) // We don't need the Uri object here, just the boolean result
            {
                containsTextUrl = true;
            }
        }

        // Decide if we should allow the drop based on text URL presence
        if (containsTextUrl)
        {
            allowDrop = true;
            // If files are also present, Copy is probably fine.
            // If *only* a URL is present, you might prefer Link, but Copy is also common.
            // Let's keep it simple: if either is valid, allow Copy.
            proposedEffect = DragDropEffects.Copy; // Or DragDropEffects.Link
        }

        // Set the final effect based on whether any valid data format was found
        e.DragEffects = allowDrop ? proposedEffect : DragDropEffects.None;

        // We've evaluated the drag operation and decided on the effect (or None).
        // Set Handled = true to prevent the event from bubbling up further.
        e.Handled = true;
    }

    /// <summary>
    /// Checks if the provided text represents a valid HTTP or HTTPS URL,
    /// including attempting to prepend "https://" if no scheme is present.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <param name="validUri">If the text is a valid URL, this outputs the corresponding absolute Uri object.</param>
    /// <returns>True if the text is considered a valid web URL, otherwise false.</returns>
    private bool IsValidWebUrl(string? text, out Uri? validUri)
    {
        validUri = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Attempt 1: Try parsing as an absolute URI directly (handles existing http/https)
        if (
            Uri.TryCreate(text, UriKind.Absolute, out var initialUri)
            && (initialUri.Scheme == Uri.UriSchemeHttp || initialUri.Scheme == Uri.UriSchemeHttps)
        )
        {
            validUri = initialUri;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles the Drop event for the literature ListBox.
    /// Processes dropped files asynchronously.
    /// </summary>
    private async void HandleLiteratureListDropAsync(object? sender, DragEventArgs e)
    {
        var processingTasks = new List<Task>();
        bool dataFound = false;

        var storageItems = e.Data.GetFiles();
        if (storageItems?.Any() == true)
        {
            dataFound = true;
            e.DragEffects = DragDropEffects.Copy; // Indicate we're handling it
            e.Handled = true;

            Logger.Info($"Processing {storageItems.Count()} dropped items...");

            foreach (var item in storageItems)
            {
                if (item is IStorageFile file) // Process only files
                {
                    processingTasks.Add(ProcessDroppedFileAsync(file));
                }
            }
        }

        if (e.Data.Contains(DataFormats.Text))
        {
            var droppedText = e.Data.Get(DataFormats.Text) as string;
            if (!string.IsNullOrWhiteSpace(droppedText))
            {
                Uri? uriResult = null; // Variable to hold the final valid URI
                bool isValidWebUrl = false; // Flag to indicate success

                // ---- Start of Updated Validation Logic ----

                // 1. First attempt: Try parsing as an absolute URI directly.
                // This handles cases where the scheme (http/https) is already present.
                if (
                    Uri.TryCreate(droppedText, UriKind.Absolute, out var initialUri)
                    && (
                        initialUri.Scheme == Uri.UriSchemeHttp
                        || initialUri.Scheme == Uri.UriSchemeHttps
                    )
                )
                {
                    // Successfully parsed as a valid HTTP or HTTPS absolute URI
                    uriResult = initialUri;
                    isValidWebUrl = true;
                    Logger.Info($"Processing dropped absolute URL: {uriResult.AbsoluteUri}");
                }
                // ---- End of Updated Validation Logic ----

                // 3. Process if a valid web URL was found either way
                if (isValidWebUrl && uriResult != null)
                {
                    dataFound = true; // Mark that we found processable data
                    // Add the processing task using the validated, absolute URI
                    processingTasks.Add(ProcessDroppedUrlAsync(uriResult.AbsoluteUri));
                }
                else
                {
                    // Log if the dropped text couldn't be interpreted as a valid web URL
                    Logger.Info(
                        $"Dropped text is not a valid HTTP/HTTPS URL or recognized schemeless URL: '{droppedText}'"
                    );
                    // Optionally, handle other types of text drops here
                }
            }
            else
            {
                Logger.Info("Dropped text is null or whitespace.");
            }
        }

        if (dataFound && processingTasks.Count != 0)
        {
            // We found something we recognize and created tasks for it.
            e.DragEffects = DragDropEffects.Copy; // Confirm the effect (should match DragOver)
            e.Handled = true; // Crucial: Mark the event as handled

            Logger.Info(
                $"Processing {processingTasks.Count} total dropped items (files and/or URLs)..."
            );

            try
            {
                await Task.WhenAll(processingTasks);
                Logger.Info("Finished processing dropped items.");

                // Optional: Trigger UI updates or save operations after all items are processed
                // Example: If _baseLiteratureItems is modified in the async methods and needs UI refresh
                // Or trigger a save command:
                // SaveLiteratureItemsToDisk(_baseLiteratureItems);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while processing dropped items.");
                // Use Dispatcher if ShowWarningDialog needs to run on UI thread from background task exception
                await Dispatcher.UIThread.InvokeAsync(
                    () =>
                        MessageDialog.ShowWarningDialog(
                            $"An error occurred while processing dropped items: {ex.Message}"
                        )
                );
            }
        }
        else
        {
            // Nothing we could process was found in the drop data.
            Logger.Info("Drop event received, but no compatible files or text URLs found.");
            e.Handled = false; // Let the event bubble up or indicate failure.
        }
    }

    private class LLMDATA
    {
        public string? Title { get; set; } // Use string? for nullable reference types
        public List<string>? Tags { get; set; } // Assuming Tags is an array of strings
    }

    /// <summary>
    /// Asynchronous logic to process a dropped URL.
    /// </summary>
    private async Task ProcessDroppedUrlAsync(string url)
    {
        Logger.Info($"[Simulated] Start processing URL: {url}");

        var llm = LargeLanguageManager.Instance;

        var prompt =
            $"Generate me a title and a list of (A MAXIMUM OF 4)tags to categorize the content on the website({url}), RETURN ONLY YOUR GENERATED TITLE, NOTHING ELSE, RETURN THE TITLE, PUBLISHER, AUTHOR, THE TAGS AND A SHORT SUMMARY AS JSON WITH THE KEYS 'Tags' AND 'Title', 'Publisher', 'Author', 'Summary'";
        var output = await llm.GetResponseFromUrlAsync(url, prompt);

        var newItem = new WebSourceItem(url, name: output?.Title ?? url)
        {
            Tags = output?.Tags ?? [],
            Publisher = output?.Publisher ?? "",
            Author = output?.Author ?? "",
            Summary = output?.Summary ?? "",
        };
        await Dispatcher.UIThread.InvokeAsync(() => _baseLiteratureItems.Add(newItem));

        Logger.Info($"[Simulated] Finished processing URL: {url}");
    }

    /// <summary>
    /// Processes a single dropped file: copies it, creates a LiteratureSourceItem, and adds it.
    /// </summary>
    private async Task ProcessDroppedFileAsync(IStorageFile storageFile)
    {
        string? sourcePath = storageFile.TryGetLocalPath();
        ContentMetadata? metaData = null;
        if (string.IsNullOrEmpty(sourcePath))
        {
            Logger.Warn(
                $"Could not get local path for dropped file: {storageFile.Name}. Skipping."
            );
            return;
        }

        // TODO: Add check for supported file types if necessary (e.g., only PDFs)
        // string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        // if (extension != ".pdf") { Logger.Info($"Skipping non-PDF file: {sourcePath}"); return; }

        Logger.Debug($"Processing dropped file: {sourcePath}");

        try
        {
            //TODO: We want to implement a special case for markdown files that are part of an obsidian vault,
            //instead of copying the file we want to link to it instead, maybe creating a shortcut instead?


            // 1. Copy file to the literature directory
            string? newPath = await Task.Run(() => CopyFileToLiteratureFolder(sourcePath)); // Run copy operation in background
            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }

            // 2. Create LiteratureSourceItem (LocalFileSourceItem)
            // The constructor handles basic validation and sets the default name
            var newItem = new LocalFileSourceItem(newPath);

            if (Path.GetExtension(newPath) == ".pdf")
            {
                try
                {
                    PdfMetadata info = PdfSharpMetadataExtractor.ExtractMetadata(newPath);
                    newItem.PageCount = info.PageCount;
                }
                catch (Exception ex)
                {
                    Logger.Info($"Exception was thrown {ex}");
                }

                if (newItem.PageCount >= 1000)
                {
                    MessageDialog.ShowWarningDialog(
                        $"Only pdfs smaller than 1000 pages can be used to generate metadata via a LLM"
                    );
                }
                else
                {
                    // Here we are passing a pdf that is smaller than 1000 pages
                    metaData = await LargeLanguageManager.Instance.GenerateMetaDataBasedOnFile(
                        newPath
                    );
                }
            }
            else
            {
                metaData = await LargeLanguageManager.Instance.GenerateMetaDataBasedOnFile(newPath);
            }

            newItem.SourceType = LiteratureSourceType.LocalFile;
            newItem.Title = metaData?.Title ?? storageFile.Name;
            newItem.Author = metaData?.Author ?? "";
            newItem.Tags = metaData?.Tags ?? [];
            newItem.Summary = metaData?.Summary ?? "";

            // You might want to add default tags or prompt user later

            // 3. Add to collection on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _baseLiteratureItems.Add(newItem);
                Logger.Info($"Added literature source from drop: {newItem.Name}");
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error processing dropped file '{sourcePath}'");
            // Show error to user on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageDialog.ShowErrorDialog(
                    $"Error adding file '{Path.GetFileName(sourcePath)}': {ex.Message}"
                );
            });
        }
    }

    /// <summary>
    /// Copies a file to the application's literature folder, ensuring a unique name.
    /// </summary>
    /// <returns>The full path to the newly copied file, or null if an error occurred.</returns>
    private static string? CopyFileToLiteratureFolder(string sourcePath)
    {
        try
        {
            string literaturePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                LITERATURE_FOLDER_NAME
            );
            string destinationFileName = Path.GetFileName(sourcePath);
            string destinationPath = Path.Combine(literaturePath, destinationFileName);

            // Handle potential naming conflicts (e.g., append number or GUID)
            int count = 1;
            string fileNameOnly = Path.GetFileNameWithoutExtension(destinationPath);
            string extension = Path.GetExtension(destinationPath);
            while (File.Exists(destinationPath))
            {
                destinationFileName = $"{fileNameOnly}_{count}{extension}";
                destinationPath = Path.Combine(literaturePath, destinationFileName);
                count++;
            }

            File.Copy(sourcePath, destinationPath);
            Logger.Debug($"Copied '{sourcePath}' to '{destinationPath}'");
            return destinationPath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error copying file '{sourcePath}' to literature folder.");
            // Don't show dialog here, let caller handle UI feedback
            return null;
        }
    }

    /// <summary>
    /// Handles changes to the base item collection. Updates the count and triggers save asynchronously.
    /// </summary>
    private void OnBaseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LiteratureSourceItem? addedItem = null;

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Assuming only one item is added at a time from your drop logic for selection purpose
            addedItem = e.NewItems.OfType<LiteratureSourceItem>().FirstOrDefault();
            if (addedItem != null)
            {
                SubscribeToItemChanges(addedItem); // Subscribe to changes on the new item
            }
        }

        if (_isDisposed)
            return; // Double check dispose state

        // Handle the UI updates asynchronously
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;

            // Run filtering/sorting on a background thread if it's computationally expensive
            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ApplyFilterAndSort(searchText, addedItem);
                    _itemCountTextBlockBuilder?.SetText($"Items: {_baseLiteratureItems.Count}");
                });
            });
        });
    }

    // --- Actions (Implement Logic) ---

    private async Task AddSourceAsync()
    {
        // Placeholder: Show dialog/flyout to choose File or URL
        Logger.Debug("Add Source button clicked.");

        // For now, just implement adding a local file
        var options = new FilePickerOpenOptions
        {
            Title = "Select Literature File",
            AllowMultiple = true, // Allow adding multiple files at once
            // Add FileTypeFilter if you want to restrict (e.g., PDFs only)
            // FileTypeFilter = [ new FilePickerFileType("PDF Documents") { Patterns = ["*.pdf"] } ]
        };

        var result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

        if (result?.Any() == true)
        {
            var processingTasks = result.Select(file => ProcessDroppedFileAsync(file)).ToList();
            try
            {
                await Task.WhenAll(processingTasks);
                Logger.Info($"Finished adding {result.Count} file(s).");
                // Optionally trigger save
                // SaveLiteratureItemsToDisk(_baseLiteratureItems);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while adding selected files.");
                MessageDialog.ShowWarningDialog(
                    $"An error occurred while adding files: {ex.Message}"
                );
            }
        }
        else
        {
            Logger.Debug("Add Source file selection cancelled.");
        }
    }

    // --- Filtering & Sorting ---
    private void ApplyFilterAndSort(string searchText, LiteratureSourceItem? itemToSelect = null)
    {
        if (_literatureListBuilder?.Entity.IsAlive() != true)
        {
            Logger.Warn("Cannot apply filter/sort, ListBox builder is invalid.");
            return;
        }

        string lowerSearchText = searchText?.ToLowerInvariant() ?? string.Empty;

        IEnumerable<LiteratureSourceItem> filteredItems;
        if (string.IsNullOrWhiteSpace(lowerSearchText))
        {
            filteredItems = _baseLiteratureItems; // No filter
        }
        else
        {
            filteredItems = _baseLiteratureItems.Where(item =>
            {
                // Check Name
                if (
                    item.Name?.Contains(
                        lowerSearchText,
                        StringComparison.InvariantCultureIgnoreCase
                    ) ?? false
                )
                    return true;
                // Check Title
                if (
                    item.Title?.Contains(
                        lowerSearchText,
                        StringComparison.InvariantCultureIgnoreCase
                    ) ?? false
                )
                    return true;
                // Check Author
                if (
                    item.Author?.Contains(
                        lowerSearchText,
                        StringComparison.InvariantCultureIgnoreCase
                    ) ?? false
                )
                    return true;
                // Check Summary
                if (
                    item.Summary?.Contains(
                        lowerSearchText,
                        StringComparison.InvariantCultureIgnoreCase
                    ) ?? false
                )
                    return true;
                // Check Tags
                if (
                    item.Tags?.Any(tag =>
                        tag?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase)
                        ?? false
                    ) ?? false
                )
                    return true;
                // Check FilePath (if applicable)
                if (
                    item is LocalFileSourceItem fileItem
                    && (
                        fileItem.FilePath?.Contains(
                            lowerSearchText,
                            StringComparison.InvariantCultureIgnoreCase
                        ) ?? false
                    )
                )
                    return true;
                // Check URL (if applicable)
                if (
                    item is WebSourceItem webItem
                    && (
                        webItem.Url?.Contains(
                            lowerSearchText,
                            StringComparison.InvariantCultureIgnoreCase
                        ) ?? false
                    )
                )
                    return true;

                return false;
            });
        }

        // TODO: Add Sorting Logic if needed (using _sortComboBoxBuilder)
        // IEnumerable<LiteratureSourceItem> sortedAndFilteredItems = ApplySorting(filteredItems);
        IEnumerable<LiteratureSourceItem> sortedAndFilteredItems = filteredItems.OrderBy(item =>
            item.Name
        ); // Default sort by Name

        // Update the ListBox ItemsSource on the UI thread
        var finalCollection = new ObservableCollection<LiteratureSourceItem>(
            sortedAndFilteredItems
        );
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_literatureListBuilder?.Entity.IsAlive() == true)
                {
                    _literatureListBuilder.SetItemsSource(finalCollection);
                    Logger.Trace(
                        $"ListBox updated. Filter: '{searchText}', Items displayed: {finalCollection.Count}"
                    );

                    if (itemToSelect != null && finalCollection.Contains(itemToSelect)) // Check it's in the filtered list
                    {
                        _literatureListBuilder.SetSelectedItem(itemToSelect);

                        // Optional: Scroll the newly selected item into view
                        var listBoxControl = _literatureListBuilder.Get<ListBox>();
                        Dispatcher.UIThread.Post(
                            () => // Post again ensure layout is updated before scrolling
                            {
                                if (listBoxControl?.IsVisible == true)
                                    listBoxControl.ScrollIntoView(itemToSelect);
                            },
                            DispatcherPriority.Loaded
                        ); // Use Loaded priority for scrolling after layout

                        Logger.Info($"Item '{itemToSelect.Name}' selected after filter/sort.");
                    }
                }
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Removes items with duplicate FilePaths from the collection,
    /// keeping the first occurrence encountered for each FilePath.
    /// Uses LINQ GroupBy to identify duplicates.
    /// Case-insensitive comparison for FilePath. Handles null FilePaths (treats nulls as distinct from each other unless multiple items have null path).
    /// </summary>
    public void RemoveDuplicateFilePaths()
    {
        Console.WriteLine("\n--- Running RemoveDuplicateFilePaths_UsingGroupBy ---");
        // 1. Use LINQ to identify items to remove
        //    - Group by FilePath (case-insensitive, handle nulls)
        //    - Filter groups having more than one item (duplicates)
        //    - From each duplicate group, select all items EXCEPT the first one
        //    - Collect these items into a list
        var itemsToRemove = _baseLiteratureItems
            .OfType<LocalFileSourceItem>()
            .GroupBy(item => item?.FilePath, StringComparer.OrdinalIgnoreCase) // Group by FilePath, case-insensitive
            .Where(group => group.Count() > 1) // Find groups with more than one item
            .SelectMany(group => group.Skip(1)) // Select all but the first item from each duplicate group
            .ToList(); // Materialize the list of items to remove BEFORE modifying the collection

        // 2. Remove the identified items from the ObservableCollection
        if (itemsToRemove.Count != 0)
        {
            Console.WriteLine($"Found {itemsToRemove.Count} duplicate item(s) to remove:");
            // It's crucial to remove items using the collection's Remove method
            // to trigger CollectionChanged notifications correctly.
            foreach (var itemToRemove in itemsToRemove)
            {
                Console.WriteLine($"- Removing: {itemToRemove}");
                _baseLiteratureItems.Remove(itemToRemove); // This triggers notification
            }
            Console.WriteLine("Duplicates removed.");
        }
        else
        {
            Console.WriteLine("No duplicate file paths found to remove.");
        }
    }

    // --- Persistence ---

    /// <summary>
    /// Loads literature items from the JSON save file.
    /// </summary>
    private ObservableCollection<LiteratureSourceItem> LoadLiteratureItemsFromDisk()
    {
        string filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            LITERATURE_FOLDER_NAME,
            LITERATURE_SAVE_FILE
        );
        if (!File.Exists(filePath))
        {
            Logger.Info(
                $"Literature save file not found ('{filePath}'). Starting with empty collection."
            );
            return [];
        }

        try
        {
            Logger.Info($"Loading literature items from '{filePath}'...");
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // Match saving format
                // *** IMPORTANT: Add converter for abstract base class ***
                Converters =
                {
                    new LiteratureSourceItemConverter(),
                } // You need to create this converter
                ,
            };

            var items = JsonSerializer.Deserialize<ObservableCollection<LiteratureSourceItem>>(
                jsonString,
                options
            );
            Logger.Info($"Successfully loaded {items?.Count ?? 0} literature items.");
            return items ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error(
                ex,
                $"Error loading literature items from '{filePath}'. Returning empty collection."
            );
            MessageDialog.ShowErrorDialog(
                $"Error loading literature data: {ex.Message}\n\nA new empty list will be used."
            );
            return [];
        }
    }

    /// <summary>
    /// Saves the current literature items to the JSON save file.
    /// </summary>
    public static async Task SaveLiteratureItemsToDiskAsync(
        ObservableCollection<LiteratureSourceItem> items
    )
    {
        if (items == null)
            return;

        string directoryPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            LITERATURE_FOLDER_NAME
        );
        string filePath = Path.Combine(directoryPath, LITERATURE_SAVE_FILE);

        try
        {
            Logger.Info($"Saving {items.Count} literature items to '{filePath}'...");
            Directory.CreateDirectory(directoryPath); // Ensure directory exists

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // *** IMPORTANT: Add converter for abstract base class ***
                Converters =
                {
                    new LiteratureSourceItemConverter(),
                } // You need to create this converter
                ,
            };

            string jsonString = JsonSerializer.Serialize(items, options);
            await File.WriteAllTextAsync(filePath, jsonString);
            Logger.Info("Successfully saved literature items.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error saving literature items to '{filePath}'.");
            MessageDialog.ShowErrorDialog($"Error saving literature data: {ex.Message}");
        }
    }

    // --- IDisposable Implementation ---
    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposable
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Logger.Debug($"Disposing LiteraturePage (Root: {_root.Id})...");

                // Unsubscribe external events
                if (_baseLiteratureItems != null)
                {
                    _baseLiteratureItems.CollectionChanged -= OnBaseCollectionChanged;
                }
                var mainWindow = App.GetMainWindow();
                if (mainWindow != null)
                {
                    mainWindow.Closing -= async (_, _) =>
                        await SaveLiteratureItemsToDiskAsync(_baseLiteratureItems!);
                }

                // Dispose internal disposables (includes timer entities if added)
                _disposables.Dispose();

                // Destroy root Flecs UI if owned by this page
                if (_root.IsValid() && _root.IsAlive())
                {
                    // Flecs OnRemove hooks should handle component disposal
                    _root.Destruct();
                }

                Logger.Debug("LiteraturePage disposed.");
            }
            _isDisposed = true;
        }
    }

    // --- IUIComponent Implementation ---
    /// <inheritdoc/>
    public void Attach(Entity parent)
    {
        if (_root.IsValid() && !_root.Has(Ecs.ChildOf, parent))
        {
            _root.ChildOf(parent);
        }
    }

    /// <inheritdoc/>
    public void Detach()
    {
        if (_root.IsValid() && _root.Parent().IsValid())
        {
            _root.Remove(Ecs.ChildOf, _root.Parent());
        }
    }
}

/*

// --- TODO: Create this JsonConverter ---
// You will need a custom System.Text.Json.Serialization.JsonConverter
// to handle serializing and deserializing the abstract LiteratureSourceItem
// and its derived types (LocalFileSourceItem, WebSourceItem, etc.).
// This converter will typically look at the 'SourceType' property during
// deserialization to decide which concrete class to instantiate.
public class LiteratureSourceItemConverter : System.Text.Json.Serialization.JsonConverter<LiteratureSourceItem>
{
    public override LiteratureSourceItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Implementation needed: Read JSON, determine SourceType, deserialize into the correct derived type.
        throw new NotImplementedException("Deserialization logic for LiteratureSourceItem hierarchy is required.");
    }

    public override void Write(Utf8JsonWriter writer, LiteratureSourceItem value, JsonSerializerOptions options)
    {
        // Implementation needed: Serialize the actual derived type.
        // Using JsonSerializer.Serialize(writer, (object)value, value.GetType(), options)
        // within this method often works if the derived types are simple POCOs
        // or if you remove the converter temporarily from options for the recursive call.
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options); // May need adjusted options
                                                                                   // throw new NotImplementedException("Serialization logic for LiteratureSourceItem hierarchy is required.");
    }

    // Optional: Add CanConvert if needed, though usually not required if applied directly
    // public override bool CanConvert(Type typeToConvert) => typeof(LiteratureSourceItem).IsAssignableFrom(typeToConvert);
}
*/
