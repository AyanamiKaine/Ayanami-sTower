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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage; // For FilePicker
using Avalonia.Threading;
using AyanamisTower.StellaLearning.Data;
using AyanamisTower.StellaLearning.Util;
using AyanamisTower.Toast;
using CommunityToolkit.Mvvm.ComponentModel;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module; // If Page tag is defined here

namespace AyanamisTower.StellaLearning.Pages;

/*
TODO: Currently the file reaches over 1300 LOC this indicates that we can probably refactor somethings into
their own components. Making this smaller and more maintainable.
*/

/*

We want to be able to easily right click on a refrence to copy it to the clip
board so we can past it into our drawing program.

We should be able to open our refrence folder in the app, as well as other folders.

*/


/// <summary>
/// Represents the data structure for storing reference painting metadata in JSON.
/// </summary>
public class ReferencePaintingMetadata
{
    /// <summary>
    /// The full path to the image file. This is the key identifier.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// The user-defined or initially parsed display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The list of tags associated with the reference image.
    /// </summary>
    public List<string> Tags { get; set; } = []; // Initialize to avoid null issues
}

/// <summary>
/// Represents a reference painting for study.
/// </summary>
public partial class ReferencePaintingItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the name of the reference painting.
    /// </summary>
    [ObservableProperty]
    private string _name = "Unnamed Reference";

    /// <summary>
    /// Gets or sets the file path to the reference painting image.
    /// </summary>
    [ObservableProperty]
    private string _imagePath = string.Empty;

    /// <summary>
    /// Gets or sets a cached thumbnail of the reference painting.
    /// </summary>
    [ObservableProperty]
    private Bitmap? _thumbnail; // Cache thumbnail for performance

    [ObservableProperty]
    private ObservableCollection<string> _tags = [];
}

/// <summary>
/// Represents a study (copy, sketch) linked to a reference painting.
/// </summary>
public partial class ArtStudyItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the name of the art study.
    /// </summary>
    [ObservableProperty]
    public string _name = "Unnamed Study";

    /// <summary>
    /// Gets or sets the file path to the art study image.
    /// </summary>
    [ObservableProperty]
    public string _imagePath = string.Empty;

    /// <summary>
    /// Gets or sets the type of study.
    /// </summary>
    [ObservableProperty]
    public StudyType _type;

    /// <summary>
    /// Gets or sets a cached thumbnail of the study.
    /// </summary>
    [ObservableProperty]
    public Bitmap? _thumbnail;
}

/// <summary>
/// Type of art study.
/// </summary>
public enum StudyType
{
    /// <summary>
    /// A detailed study that attempts to recreate a master artwork.
    /// </summary>
    MasterCopy,

    /// <summary>
    /// A quick sketch created in a limited timeframe.
    /// </summary>
    SpeedSketch,

    /// <summary>
    /// A small sketch that captures the essence of the subject.
    /// </summary>
    ThumbnailSketch,

    /// <summary>
    /// Any other type of art study not covered by the specific categories.
    /// </summary>
    Other,
}

// --- ArtPage Component ---

/// <summary>
/// Art page, used for art studies, like master copies, collection of sketches.
/// </summary>
public class ArtPage : IUIComponent, IDisposable
{
    private readonly World _world;
    private Entity _root;
    private bool _isDisposed = false;

    // --- UI Element Builders (for easy access) ---
    private UIBuilder<ListBox>? _referenceListBuilder;

    // --- Data Collections ---
    private readonly ObservableCollection<ReferencePaintingItem> _referencePaintings = [];
    private readonly ObservableCollection<ArtStudyItem> _currentStudies = [];

    // --- State ---
    private ReferencePaintingItem? _selectedReference = null;

    // --- Constants ---
    private const string METADATA_FILE_NAME = "references_metadata.json";
    private const string ART_FOLDER_NAME = "art";
    private const string REFERENCES_SUBFOLDER = "references";
    private const string STUDIES_SUBFOLDER = "studies";
    private const int THUMBNAIL_SIZE = 100; // Size for thumbnails in lists

    // --- Fields for Hover Preview ---
    private Flyout? _previewFlyout;
    private Image? _previewImage;
    private Control? _flyoutTargetControl = null; // Keep track of what the flyout is attached to
    private Image? _currentHoveredImage = null;
    private ReferencePaintingItem? _currentHoveredItem = null;
    private const int PREVIEW_MAX_SIZE = 400; // Max width/height for preview image pixels
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".webp",
    };

    /// <inheritdoc/>
    public Entity Root => _root;

    /// <summary>
    /// Art page constructor. Initializes the UI layout.
    /// </summary>
    public ArtPage(World world)
    {
        _world = world;

        // Ensure base art directories exist
        EnsureArtDirectories();
        InitializeHoverPreview();
        // Load existing data (Placeholder)
        LoadReferencePaintings(); // Implement this method

        _root = _world
            .UI<Grid>(BuildArtPageUI)
            .Add<Page>() // Add the Page tag
            .Entity;

        App.GetMainWindow().Closing += async (_, _) => await SaveReferenceMetadataAsync();
    }

    /// <summary>
    /// Builds the UI structure for the ArtPage using UIBuilder.
    /// </summary>
    private void BuildArtPageUI(UIBuilder<Grid> grid)
    {
        grid.SetRowDefinitions("Auto, Auto, *");

        grid.Child<StackPanel>(buttonsPanel =>
        {
            buttonsPanel
                .SetRow(0)
                .SetOrientation(Orientation.Horizontal)
                .SetSpacing(10)
                .SetMargin(5) // Margin below buttons
                .SetHorizontalAlignment(HorizontalAlignment.Left);

            buttonsPanel.Child<Button>(button =>
            {
                button
                    .SetText("Add Reference...")
                    .AttachToolTip(
                        _world.UI<ToolTip>(
                            (toolTip) =>
                            {
                                toolTip.Child<TextBlock>(
                                    (textBlock) =>
                                    {
                                        textBlock.SetText(
                                            "You can also drag and drop images directly in the list"
                                        );
                                    }
                                );
                            }
                        )
                    )
                    .OnClick(async (s, e) => await AddReferencePainting());
            });

            buttonsPanel.Child<Button>(button =>
            {
                // Disable initially until a reference is selected
                button
                    .SetText("Add Study/Sketch...")
                    .Disable() // Start disabled
                    .OnClick(async (s, e) => await AddArtStudy());

                // Store builder reference to enable/disable later
                // (Requires a way to access/update the button entity's InputElement component)
                // Or manage enabled state via Flecs events/components.
            });
            // Add more buttons if needed (e.g., for different sketch types)
        });

        // --- Title (Row 1) ---
        grid.Child<TextBlock>(title =>
        {
            title
                .SetRow(1) // Assign to Row 1
                .SetText("References")
                .SetFontWeight(FontWeight.Bold)
                .SetMargin(5);
        });

        // --- ScrollViewer for the List (Row 2, takes remaining space *) ---
        grid.Child<ScrollViewer>(scrollViewer =>
        {
            scrollViewer.SetMargin(5).SetRow(2);

            // ListBox
            scrollViewer.Child<ListBox>(listBox =>
            {
                _referenceListBuilder = listBox; // Store builder reference

                var contextFlyout = _world.UI<MenuFlyout>(
                    (menuFlyout) =>
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

                        menuFlyout.Child<MenuItem>(
                            (item) =>
                            {
                                // Renames the list entry and its file name
                                item.SetHeader("Open")
                                    .OnClick(
                                        (sender, _) =>
                                        {
                                            var item =
                                                listBox.GetSelectedItem<ReferencePaintingItem>();

                                            try
                                            {
                                                FileOpener.OpenFileWithDefaultProgram(
                                                    item.ImagePath
                                                );
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.Error.WriteLine(
                                                    $"Something went wrong {ex.Message}"
                                                );
                                            }
                                        }
                                    );

                                item.AttachToolTip(
                                    _world.UI<ToolTip>(
                                        (toolTip) =>
                                        {
                                            toolTip.Child<TextBlock>(
                                                (textBlock) =>
                                                {
                                                    textBlock.SetText(
                                                        "You can also double click on the item to open it!"
                                                    );
                                                }
                                            );
                                        }
                                    )
                                );
                            }
                        );

                        menuFlyout.Child<MenuItem>(item =>
                        {
                            item.SetHeader("Open Folder")
                                .OnClick(
                                    (_, _) =>
                                    {
                                        var item =
                                                listBox.GetSelectedItem<ReferencePaintingItem>();

                                        FileExplorerHelper.OpenFolderAndSelectFile(item.ImagePath);
                                    }
                                );

                            item.AttachToolTip(_world.UI<ToolTip>((toolTip) =>
                            {
                                toolTip.Child<TextBlock>((textBlock) => textBlock.SetText("Opens the folder where the images resides"));
                            })
                            );
                        });

                        menuFlyout.Child<MenuItem>(
                            (item) =>
                            {
                                // Renames the list entry and its file name
                                item.SetHeader("Rename")
                                    .OnClick(
                                        (sender, _) =>
                                        {
                                            var item =
                                                listBox.GetSelectedItem<ReferencePaintingItem>();

                                            // Ensure we have the ListBox control instance
                                            if (listBox == null)
                                                return;

                                            // Get the item associated with the context menu click
                                            // The DataContext of the MenuItem should be the ReferencePaintingItem
                                            var itemToRename =
                                                (sender as Control)?.DataContext
                                                    as ReferencePaintingItem
                                                ?? listBox.GetSelectedItem<ReferencePaintingItem>(); // Fallback

                                            if (itemToRename == null)
                                                return;

                                            // Find the ListBoxItem container for positioning the flyout
                                            var container = listBox
                                                .Get<ListBox>()
                                                .ContainerFromItem(itemToRename);
                                            if (container is Control targetControl)
                                            {
                                                ShowRenameFlyout(targetControl, itemToRename);
                                            }
                                            else
                                            {
                                                // Fallback: Show attached to the ListBox itself if container not found
                                                ShowRenameFlyout(
                                                    listBox.Get<ListBox>(),
                                                    itemToRename
                                                );
                                            }
                                        }
                                    );
                            }
                        );

                        menuFlyout.Child<MenuItem>(
                            (item) =>
                            {
                                // Uses a large language model to autogenerate tags.
                                item.SetHeader("Generate Tags")
                                    .OnClick(
                                        async (sender, _) =>
                                        {
                                            var item =
                                                listBox.GetSelectedItem<ReferencePaintingItem>();

                                            var llm = LargeLanguageManager.Instance;
                                            // Get the new tags (assuming this returns List<string> or similar)
                                            var toast = ToastService.Show("Generating metadata please wait...", NotificationType.Information);

                                            var newTagsList = await llm.GetImageTagsAsync(
                                                item.ImagePath,
                                                6
                                            );
                                            await toast.DismissAsync();
                                            ToastService.Show("Successfully generating metadata", NotificationType.Success, TimeSpan.FromSeconds(2));

                                            // --- Modification Start ---
                                            // Instead of: item.Tags = newTagsList ?? [];

                                            // Modify the existing ObservableCollection:
                                            item.Tags.Clear(); // Clear the current tags

                                            if (newTagsList != null) // Check if LLM returned tags
                                            {
                                                foreach (var tag in newTagsList)
                                                {
                                                    item.Tags.Add(tag); // Add new tags one by one
                                                }
                                            }
                                            await SaveReferenceMetadataAsync();
                                        }
                                    );
                            }
                        );

                        menuFlyout.Child<MenuItem>(
                            (item) =>
                            {
                                //Removes the refrence from the list
                                //Should not delete the file!
                                item.SetHeader("Remove")
                                    .OnClick(
                                        async (_, _) => // Make the lambda async if you plan to add async dialogs
                                        {
                                            if (listBox == null)
                                                return;

                                            var itemToRemove =
                                                listBox.GetSelectedItem<ReferencePaintingItem>();
                                            if (itemToRemove != null)
                                            {
                                                string filePath = itemToRemove.ImagePath;
                                                string displayName = itemToRemove.Name; // For use in messages

                                                // Basic validation
                                                if (string.IsNullOrEmpty(filePath))
                                                {
                                                    MessageDialog.ShowErrorDialog(
                                                        $"Error: ImagePath is missing for item '{displayName}'. Cannot delete."
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
                                                    // 1. Attempt to delete the file from disk FIRST
                                                    File.Delete(filePath);
                                                    //Console.WriteLine($"Successfully deleted file: {filePath}");

                                                    // 2. If file deletion succeeded, THEN update UI and collection
                                                    // Clear details view if the deleted item was the selected one
                                                    if (
                                                        ReferenceEquals(
                                                            _selectedReference,
                                                            itemToRemove
                                                        )
                                                    )
                                                    {
                                                        _selectedReference = null;
                                                        _currentStudies.Clear();
                                                        // TODO: Consider disabling "Add Study" button state here
                                                    }

                                                    // Remove the item from the observable collection
                                                    _referencePaintings.Remove(itemToRemove);
                                                    await SaveReferenceMetadataAsync();
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
                                        }
                                    );
                            }
                        );
                    }
                );
                listBox
                    .SetItemsSource(_referencePaintings)
                    .SetItemTemplate(CreateReferenceItemTemplate())
                    .SetSelectionMode(SelectionMode.Single)
                    .SetContextFlyout(contextFlyout)
                    .OnSelectionChanged(ReferenceList_SelectionChanged)
                    .AllowDrop()
                    .OnDragOver(HandleReferenceListDragOver)
                    .OnDrop(HandleReferenceListDropAsync)
                    .OnDoubleTapped(
                        (_, _) =>
                        {
                            var item = listBox.GetSelectedItem<ReferencePaintingItem>();

                            try
                            {
                                FileOpener.OpenFileWithDefaultProgram(item.ImagePath);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Something went wrong {ex.Message}");
                            }
                        }
                    );
                // DockPanel fills remaining space by default
            });
        });
    }

    // --- Item Template Creation ---

    private FuncDataTemplate<ReferencePaintingItem> CreateReferenceItemTemplate()
    {
        return _world.CreateTemplate<ReferencePaintingItem, Grid>(
            (grid, item) =>
            {
                if (item is null)
                    return;
                grid.SetColumnDefinitions("Auto, *, Auto").SetRowDefinitions("Auto").SetMargin(5);

                if (item.Thumbnail != null)
                {
                    // Thumbnail Image
                    grid.Child<Image>(img =>
                    {
                        img.SetColumn(0)
                            .SetBinding(
                                Image.SourceProperty,
                                nameof(ReferencePaintingItem.Thumbnail)
                            )
                            .SetWidth(THUMBNAIL_SIZE / 2.0) // Smaller thumbnail in list
                            .SetHeight(THUMBNAIL_SIZE / 2.0)
                            .SetStretch(Stretch.UniformToFill)
                            .SetVerticalAlignment(VerticalAlignment.Center)
                            .SetHorizontalAlignment(HorizontalAlignment.Left)
                            //TODO: There is a big chance that a tooltip with a image attached
                            // is much closer to the thing we want.
                            .OnPointerEntered(HandleItemPointerEntered)
                            .SetIsHitTestVisible(true);
                        // Name TextBlock
                        grid.Child<TextBlock>(txt =>
                        {
                            txt.SetTextWrapping(TextWrapping.NoWrap)
                                .SetTextTrimming(TextTrimming.CharacterEllipsis)
                                .SetColumn(1)
                                .SetMargin(10, 0, 0, 0)
                                .SetBinding(
                                    TextBlock.TextProperty,
                                    nameof(ReferencePaintingItem.Name)
                                )
                                .SetVerticalAlignment(VerticalAlignment.Center)
                                .SetHorizontalAlignment(HorizontalAlignment.Left)
                                .SetIsHitTestVisible(false);
                        });
                    });
                }

                // Row 1, Col 2: Tags (using ItemsControl or similar)
                grid.Child<ItemsControl>(tagsList =>
                {
                    tagsList
                        .SetColumn(2)
                        .SetHorizontalAlignment(HorizontalAlignment.Right)
                        .SetVerticalAlignment(VerticalAlignment.Center)
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
                                        .SetMargin(2)
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
    /// Shows a flyout with a TextBox to rename the given item, attached to the target control.
    /// </summary>
    /// <param name="targetControl">The control to attach the flyout to (ideally the ListBoxItem).</param>
    /// <param name="itemToRename">The ReferencePaintingItem being renamed.</param>
    private void ShowRenameFlyout(Control targetControl, ReferencePaintingItem itemToRename)
    {
        // Create the TextBox for renaming
        var renameTextBox = new TextBox
        {
            Text = itemToRename.Name, // Pre-populate with current name
            MinWidth = 150, // Give it some initial width
            AcceptsReturn = false, // Prevent multi-line names
        };

        // Create the Flyout to host the TextBox
        var renameFlyout = new Flyout
        {
            Content = renameTextBox,
            Placement = PlacementMode.BottomEdgeAlignedLeft, // Position it nicely below the item
            ShowMode =
                FlyoutShowMode.Transient // Hide when clicking outside
            ,
        };

        // --- Event Handlers for Confirmation/Cancellation ---
        EventHandler<KeyEventArgs>? keyDownHandler = null;
        EventHandler<RoutedEventArgs>? lostFocusHandler = null;
        bool confirmed = false; // Flag to prevent LostFocus cancel after Enter confirm

        keyDownHandler = (s, kargs) =>
        {
            if (kargs.Key == Key.Enter)
            {
                confirmed = true; // Mark as confirmed
                string newName = renameTextBox.Text.Trim();
                renameFlyout.Hide(); // Hide the flyout first

                if (!string.IsNullOrWhiteSpace(newName) && newName != itemToRename.Name)
                {
                    // Use Task.Run for file operation, but update UI item directly
                    RenameReferenceItem(itemToRename, newName); // Call the rename logic
                }
                kargs.Handled = true; // Mark event as handled
            }
            else if (kargs.Key == Key.Escape)
            {
                renameFlyout.Hide(); // Just hide on Escape
                kargs.Handled = true;
            }

            // Clean up handlers after flyout closes if needed (though GC should handle it)
            // renameFlyout.Closed += (sender, e) => {
            //    renameTextBox.KeyDown -= keyDownHandler;
            //    renameTextBox.LostFocus -= lostFocusHandler;
            // };
        };

        lostFocusHandler = (s, rargs) =>
        {
            // If rename wasn't confirmed via Enter, LostFocus acts as cancel
            if (!confirmed)
            {
                renameFlyout.Hide();
            }
            // Clean up handlers (optional, GC should manage)
            // renameTextBox.KeyDown -= keyDownHandler;
            // renameTextBox.LostFocus -= lostFocusHandler;
        };

        renameTextBox.KeyDown += keyDownHandler;
        renameTextBox.LostFocus += lostFocusHandler; // Hide on focus loss (acts as cancel if not confirmed)

        // Show the flyout attached to the target control (e.g., the ListBoxItem)
        renameFlyout.ShowAt(targetControl);

        // --- Focus the TextBox after the Flyout opens ---
        // Use Dispatcher.UIThread.Post to ensure the flyout is ready
        Dispatcher.UIThread.Post(
            () =>
            {
                renameTextBox.Focus();
                renameTextBox.SelectAll(); // Select existing text for easy replacement
            },
            DispatcherPriority.Input
        );
    }

    /// <summary>
    /// Renames the underlying file and updates the ReferencePaintingItem.
    /// Runs file operations potentially off the UI thread.
    /// </summary>
    /// <param name="item">The item to rename.</param>
    /// <param name="newName">The desired new name (without extension).</param>
    private async void RenameReferenceItem(ReferencePaintingItem item, string newName)
    {
        string oldPath = item.ImagePath;
        string? directory = Path.GetDirectoryName(oldPath);
        string extension = Path.GetExtension(oldPath); // Keep the original extension

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(extension))
        {
            MessageDialog.ShowErrorDialog($"Error: Could not parse path components for {oldPath}");
            return;
        }

        // Try to preserve the GUID prefix if it exists (assuming format GUID_Name.ext)
        string oldFileNameWithoutExtension = Path.GetFileNameWithoutExtension(oldPath);
        string newFileNameWithoutExtension;
        string guidPrefix = "";

        var parts = oldFileNameWithoutExtension.Split('_', 2);
        if (parts.Length == 2 && Guid.TryParse(parts[0], out _)) // Check if first part is a GUID
        {
            guidPrefix = parts[0] + "_"; // Keep the GUID and the underscore
            newFileNameWithoutExtension = guidPrefix + newName;
        }
        else
        {
            // If no recognizable GUID prefix, just use the new name.
            // Consider adding a GUID if one wasn't present before for uniqueness?
            // For now, just use the new name directly.
            newFileNameWithoutExtension = newName;
        }

        string newFileName = newFileNameWithoutExtension + extension;
        string newPath = Path.Combine(directory, newFileName);

        // Check if the name is actually changing and if the new file already exists
        if (newPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
        {
            item.Name = newName; // Ensure ObservableObject updates UI even if file doesn't change case etc.
            return; // No file operation needed
        }

        if (File.Exists(newPath))
        {
            MessageDialog.ShowErrorDialog(
                $"Error: Cannot rename. File already exists at {newPath}"
            );
            return;
        }

        try
        {
            // Perform file move (rename) potentially off the UI thread
            await Task.Run(() => File.Move(oldPath, newPath));

            // --- Update item properties on the UI thread ---
            // Although Task.Run was used, this continuation might run on any thread.
            // It's safer to ensure UI property updates happen on the UI thread.
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                item.ImagePath = newPath; // Update the stored path
                item.Name = newName; // Update the observable property (UI will refresh)
                //Console.WriteLine($"Successfully renamed '{Path.GetFileName(oldPath)}' to '{newFileName}'");
                await SaveReferenceMetadataAsync();
            });
        }
        catch (Exception ex)
        {
            MessageDialog.ShowErrorDialog(
                $"Error renaming file from '{oldPath}' to '{newPath}': {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Creates the DataTemplate for the art studies ListBox.
    /// </summary>
    private IDataTemplate CreateStudyItemTemplate()
    {
        return _world.CreateTemplate<ArtStudyItem, StackPanel>(
            (builder, item) =>
            {
                builder.SetOrientation(Orientation.Horizontal).SetSpacing(5);

                // Thumbnail Image
                builder.Child<Image>(img =>
                {
                    img.SetWidth(THUMBNAIL_SIZE)
                        .SetHeight(THUMBNAIL_SIZE)
                        .SetStretch(Stretch.UniformToFill)
                        .SetBinding(Image.SourceProperty, nameof(ReferencePaintingItem.Thumbnail))
                        .SetVerticalAlignment(VerticalAlignment.Center);
                });

                // Details (Name, Type)
                builder.Child<StackPanel>(details =>
                {
                    details
                        .SetOrientation(Orientation.Vertical)
                        .SetVerticalAlignment(VerticalAlignment.Center);

                    details.Child<TextBlock>(nameTxt =>
                        nameTxt.SetBinding(
                            TextBlock.TextProperty,
                            nameof(ReferencePaintingItem.Name)
                        )
                    );
                    details.Child<TextBlock>(typeTxt =>
                    {
                        typeTxt
                            .SetText(item.Type.ToString()) // Display study type
                            .SetFontSize(10)
                            .SetForeground(Brushes.Gray);
                    });
                });
            }
        );
    }

    // --- Event Handlers ---

    private void ReferenceList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ReferencePaintingItem selectedItem)
        {
            _selectedReference = selectedItem;
            LoadStudiesForReference(selectedItem); // Implement this

            // Enable "Add Study" button (Requires access to the button entity/builder)
            // Find the button entity and enable it, e.g.:
            // var addButtonEntity = _root.FindEntityByName("AddStudyButton"); // Need a way to name/find entities
            // addButtonEntity?.Enable<Button>(); // Assuming Enable extension exists
        }
        else
        {
            _selectedReference = null;
            _currentStudies.Clear();

            // Disable "Add Study" button
            // Find the button entity and disable it
            // var addButtonEntity = _root.FindEntityByName("AddStudyButton");
            // addButtonEntity?.Disable<Button>();
        }
    }

    // --- Actions (Implement Logic) ---

    private async Task AddReferencePainting()
    {
        Console.WriteLine("Add Reference Painting clicked");
        var filePath = await PickImageFileAsync("Select Reference Painting");
        if (!string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine($"Selected file: {filePath}");
            // 1. Copy file to the 'art/references' directory
            var newPath = CopyFileToArtFolder(filePath, REFERENCES_SUBFOLDER);
            if (string.IsNullOrEmpty(newPath))
                return; // Copy failed

            // 2. Create ReferencePaintingItem
            var newItem = new ReferencePaintingItem
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                ImagePath = newPath,
                // Generate thumbnail (implement LoadThumbnail)
                Thumbnail = await LoadThumbnail(newPath, THUMBNAIL_SIZE),
            };

            _referencePaintings.Add(newItem);
        }
    }

    private async Task AddArtStudy()
    {
        if (_selectedReference == null)
            return; // Should be disabled, but double-check

        Console.WriteLine($"Add Study/Sketch for: {_selectedReference.Name}");
        var filePath = await PickImageFileAsync("Select Study/Sketch Image");
        if (!string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine($"Selected file: {filePath}");
            // 1. Copy file to the 'art/studies' directory
            var newPath = CopyFileToArtFolder(filePath, STUDIES_SUBFOLDER);
            if (string.IsNullOrEmpty(newPath))
                return; // Copy failed

            // 2. Determine Study Type (e.g., show a dialog)
            const StudyType studyType = StudyType.MasterCopy; // Placeholder

            // 3. Create ArtStudyItem
            var newItem = new ArtStudyItem
            {
                Name = Path.GetFileNameWithoutExtension(newPath),
                ImagePath = newPath,
                Type = studyType,
                Thumbnail = await LoadThumbnail(newPath, THUMBNAIL_SIZE),
            };

            // 4. Add to current studies collection (and save metadata)
            _currentStudies.Add(newItem);

            // 5. Save metadata about studies, linking them to references
            // SaveStudyMetadata();
        }
    }

    // --- File/Data Handling (Placeholders - Implement Robustly) ---

    /// <summary>
    /// Asynchronously saves the current state of reference painting metadata to a JSON file.
    /// </summary>
    private async Task SaveReferenceMetadataAsync()
    {
        // Prevent saving if the list hasn't been fully loaded or is mid-operation?
        // Consider adding checks if needed.

        string filePath = GetMetadataFilePath();
        //Console.WriteLine($"Attempting to save metadata to: {filePath}"); // Debug

        try
        {
            // 1. Create a list of DTOs from the current ObservableCollection
            var metadataList = _referencePaintings
                .Select(item => new ReferencePaintingMetadata
                {
                    ImagePath = item.ImagePath,
                    Name = item.Name,
                    Tags =
                    [
                        .. item.Tags,
                    ] // Directly use the Tags list
                    ,
                })
                .ToList();

            // 2. Configure JSON serializer options (optional, for readability)
            var options = new JsonSerializerOptions
            {
                WriteIndented =
                    true // Makes the JSON file human-readable
                ,
            };

            // 3. Serialize the list to JSON and write to the file asynchronously
            // Use FileStream for async writing
            await using FileStream createStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(createStream, metadataList, options);
            Console.WriteLine(
                $"Successfully saved metadata for {_referencePaintings.Count} items."
            ); // Debug
        }
        catch (Exception ex)
        {
            // Log the error or show a message to the user
            Console.WriteLine($"Error saving reference metadata to {filePath}: {ex.Message}");
            MessageDialog.ShowErrorDialog($"Failed to save reference metadata:\n{ex.Message}");
        }
    }

    private static void EnsureArtDirectories()
    {
        try
        {
            string baseArtPath = Path.Combine(Directory.GetCurrentDirectory(), ART_FOLDER_NAME);
            Directory.CreateDirectory(baseArtPath);
            Directory.CreateDirectory(Path.Combine(baseArtPath, REFERENCES_SUBFOLDER));
            Directory.CreateDirectory(Path.Combine(baseArtPath, STUDIES_SUBFOLDER));
        }
        catch (Exception ex)
        {
            MessageDialog.ShowErrorDialog($"Error creating art directories: {ex.Message}");
        }
    }

    private static string? CopyFileToArtFolder(string sourcePath, string subfolder)
    {
        try
        {
            string baseArtPath = Path.Combine(Directory.GetCurrentDirectory(), ART_FOLDER_NAME);
            string destinationFolder = Path.Combine(baseArtPath, subfolder);
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(sourcePath)}";
            string destinationPath = Path.Combine(destinationFolder, uniqueFileName);

            File.Copy(sourcePath, destinationPath);
            return destinationPath;
        }
        catch (Exception ex)
        {
            MessageDialog.ShowErrorDialog($"Error copying file to art folder: {ex.Message}");
            return null;
        }
    }

    private static async Task<string?> PickImageFileAsync(string title)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                FilePickerFileTypes.ImageAll,
            ] // Use predefined image types
            ,
        };

        var result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);
        return result?.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    private static async Task<Bitmap?> LoadThumbnail(string imagePath, int size)
    {
        // Basic thumbnail loading. Consider a more robust library or async loading
        // This is synchronous and might block UI thread for large images
        // Consider using Task.Run for decoding and Avalonia dispatcher for UI update
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(imagePath))
                    return null;

                using var stream = File.OpenRead(imagePath);
                // Decode the bitmap with a specified decode size for efficiency
                return Bitmap.DecodeToWidth(stream, size);
                // For DecodeToHeight: return Bitmap.DecodeToHeight(stream, size);
                // For more control: var original = new Bitmap(stream); return original.CreateScaledBitmap(new PixelSize(size, size)); // Might distort aspect ratio
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading thumbnail for {imagePath}: {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>
    /// Extracts a user-friendly display name from a filename,
    /// removing a leading "GUID_" prefix if present.
    /// </summary>
    /// <param name="fileName">The full filename (e.g., "guid_MyPainting.jpg" or "MyPainting.jpg").</param>
    /// <returns>The display name (e.g., "MyPainting").</returns>
    private static string GetDisplayNameFromFileName(string fileName)
    {
        // Get the part of the filename without the extension
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Check if it contains an underscore, suggesting a potential prefix
        int underscoreIndex = nameWithoutExtension.IndexOf('_');

        // If an underscore exists and the part before it looks like a GUID
        if (
            underscoreIndex > 0
            && // Ensure underscore is not the first character
            Guid.TryParse(nameWithoutExtension.Substring(0, underscoreIndex), out _)
        )
        {
            // Return the part *after* the first underscore
            return nameWithoutExtension.Substring(underscoreIndex + 1);
        }
        else
        {
            // Otherwise, return the whole filename without extension
            return nameWithoutExtension;
        }
    }

    /// <summary>
    /// Loads reference painting metadata from the JSON file, verifies files exist,
    /// generates thumbnails, and populates the _referencePaintings collection.
    /// </summary>
    private async void LoadReferencePaintings() // Changed return type to void as it starts async work
    {
        string filePath = GetMetadataFilePath();
        //Console.WriteLine($"Attempting to load metadata from: {filePath}"); // Debug

        _referencePaintings.Clear(); // Clear existing items before loading

        if (!File.Exists(filePath))
        {
            Console.WriteLine(
                $"Metadata file not found ({filePath}). No references loaded initially."
            );
            // Optionally: You could fall back to scanning the directory here if you want
            // to automatically import files found when no metadata exists.
            // LoadReferencesFromDirectoryFallback(); // Example call to a fallback method
            return;
        }

        List<ReferencePaintingMetadata>? metadataList = null;
        try
        {
            // Read and deserialize the JSON file asynchronously
            await using FileStream openStream = File.OpenRead(filePath);
            metadataList = await JsonSerializer.DeserializeAsync<List<ReferencePaintingMetadata>>(
                openStream
            );

            if (metadataList == null)
            {
                Console.WriteLine("Metadata file is empty or corrupted. No references loaded.");
                MessageDialog.ShowErrorDialog("Reference metadata file seems empty or corrupted.");
                return;
            }
            //Console.WriteLine($"Successfully deserialized {metadataList.Count} metadata items."); // Debug
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"Error deserializing metadata file {filePath}: {jsonEx.Message}");
            MessageDialog.ShowErrorDialog(
                $"Error reading reference metadata file:\n{jsonEx.Message}\n\nPlease check or delete the file '{METADATA_FILE_NAME}' in the '{ART_FOLDER_NAME}' directory."
            );
            return; // Stop loading if file is corrupt
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening metadata file {filePath}: {ex.Message}");
            MessageDialog.ShowErrorDialog($"Could not open reference metadata file:\n{ex.Message}");
            return;
        }

        // Process each loaded metadata item
        var loadTasks = new List<Task>();
        foreach (var metadata in metadataList)
        {
            // Use Task.Run to parallelize file checking and thumbnail loading
            loadTasks.Add(
                Task.Run(async () =>
                {
                    // Validate ImagePath
                    if (string.IsNullOrWhiteSpace(metadata.ImagePath))
                    {
                        Console.WriteLine(
                            $"Warning: Skipping item with missing ImagePath in metadata."
                        );
                        return; // Skip this item
                    }

                    // Check if the referenced file actually still exists
                    if (!File.Exists(metadata.ImagePath))
                    {
                        Console.WriteLine(
                            $"Warning: Image file not found for '{metadata.Name}' at '{metadata.ImagePath}'. Skipping."
                        );
                        // Optional: Show a warning to the user later, maybe aggregate these?
                        return; // Skip this item
                    }

                    // Generate thumbnail
                    Bitmap? thumbnail = await LoadThumbnail(metadata.ImagePath, THUMBNAIL_SIZE);
                    // Note: LoadThumbnail already handles its own errors, returning null

                    // Create the ReferencePaintingItem
                    var newItem = new ReferencePaintingItem
                    {
                        ImagePath = metadata.ImagePath,
                        Name = metadata.Name, // Use the name from metadata
                        Tags = new ObservableCollection<string>(metadata.Tags ?? []), // <-- Ensure this conversion
                        Thumbnail = thumbnail,
                    };

                    // Add to the collection on the UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _referencePaintings.Add(newItem);
                    });
                })
            );
        }

        // Wait for all loading tasks (optional, but good for knowing when loading is complete)
        try
        {
            await Task.WhenAll(loadTasks);
            //Console.WriteLine("Finished processing all loaded metadata items."); // Debug
        }
        catch (Exception ex)
        {
            // This catch is for potential errors within the Task.Run delegates that weren't caught inside.
            Console.WriteLine(
                $"An error occurred during the processing of loaded references: {ex.Message}"
            );
            MessageDialog.ShowWarningDialog(
                $"An error occurred while loading reference details:\n{ex.Message}"
            );
        }

        _world.Set(metadataList);
    }

    private void LoadStudiesForReference(ReferencePaintingItem reference)
    {
        // Placeholder: Load studies associated with the selected reference
        // This requires storing the association (e.g., in metadata)
        _currentStudies.Clear();
        //Console.WriteLine($"Placeholder: Load studies for {reference.Name}");
        // Example: Query metadata file/database for studies linked to reference.ImagePath or reference.EntityId
        // For now, maybe scan the studies folder and *guess* based on name? (Not reliable)

        // Example structure if you had metadata:
        // var allStudiesMetadata = LoadAllStudyMetadata();
        // var associatedStudies = allStudiesMetadata.Where(s => s.ReferenceImagePath == reference.ImagePath);
        // foreach(var studyMeta in associatedStudies) {
        //     var studyItem = new ArtStudyItem { ... populate from studyMeta ... };
        //     _currentStudies.Add(studyItem);
        // }
    }

    private void InitializeHoverPreview()
    {
        _previewImage = new Image
        {
            MaxWidth = PREVIEW_MAX_SIZE,
            MaxHeight = PREVIEW_MAX_SIZE,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(5),
        };
        _previewFlyout = new Flyout
        {
            Content = _previewImage,
            Placement = PlacementMode.LeftEdgeAlignedTop,
            // Keep this mode, it handles dismissal nicely if pointer moves away
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway,
        };

        _previewFlyout.Closed += PreviewFlyout_Closed;
    }

    private void PreviewFlyout_Closed(object? sender, EventArgs e)
    {
        // When the flyout closes (for any reason), detach it from the control
        if (_flyoutTargetControl != null)
        {
            FlyoutBase.SetAttachedFlyout(_flyoutTargetControl, null);
            //Console.WriteLine($"Flyout detached from {_flyoutTargetControl.GetType().Name}"); // Debug
            _flyoutTargetControl = null; // Clear the stored target
        }
        // Optional: Clear the image to free memory sooner
        if (_previewImage != null)
            _previewImage.Source = null;

        // Also clear hover state just in case Exited didn't fire correctly (e.g., window closed)
        _currentHoveredImage = null;
        _currentHoveredItem = null;
        // _previewLoadCts?.Cancel(); // Ensure cancellation on close if using CTS
    }

    private async void HandleItemPointerEntered(object? sender, PointerEventArgs e)
    {
        if (
            sender is not Image hoveredImage
            || hoveredImage.DataContext is not ReferencePaintingItem item
            || _previewFlyout == null
            || _previewImage == null
        )
        {
            // If sender is not the image or context is wrong, ensure any existing flyout is hidden
            _previewFlyout?.Hide();
            _currentHoveredImage = null; // Clear state
            _currentHoveredItem = null;
            // _previewLoadCts?.Cancel(); // Cancel any ongoing load if using CTS
            return;
        }

        // If pointer is already over the image we are showing/loading for, do nothing
        if (ReferenceEquals(_currentHoveredImage, hoveredImage))
        {
            return;
        }

        // Entering a new image, hide any previous flyout immediately
        _previewFlyout.Hide(); // Hides flyout attached to previous target
        _currentHoveredImage = hoveredImage; // Set new target image
        _currentHoveredItem = item; // Set new target item
        // _previewLoadCts?.Cancel(); // Cancel previous load if using CTS
        // _previewLoadCts = new CancellationTokenSource(); // Create new token if using CTS
        // var cancellationToken = _previewLoadCts.Token; // Get token if using CTS

        //Console.WriteLine($"Pointer Entered Image for: {item.Name}. Loading preview..."); // Debug

        Bitmap? fullBitmap = null;
        try
        {
            // Load the image asynchronously off the UI thread
            fullBitmap = await Task.Run(async () => // Make inner lambda async if needed
            {
                // cancellationToken.ThrowIfCancellationRequested(); // Check cancellation if using CTS
                if (!File.Exists(item.ImagePath))
                    return null;
                try
                {
                    await using var stream = File.OpenRead(item.ImagePath);
                    // Decode reasonably large but capped
                    // Consider adding cancellationToken support to async file/decode methods if possible
                    return Bitmap.DecodeToWidth(stream, PREVIEW_MAX_SIZE * 2);
                }
                catch (Exception decodeEx)
                {
                    Console.WriteLine(
                        $"Error DECODING image for preview ({item.ImagePath}): {decodeEx.Message}"
                    );
                    return null;
                }
            } /*, cancellationToken*/
            ); // Pass token if using CTS

            // --- Post-Load Check ---
            // ThrowIfCancellationRequested(); // Check cancellation if using CTS

            // Check if we are STILL hovering over the SAME image after the async load completed
            if (
                !ReferenceEquals(_currentHoveredImage, hoveredImage)
                || // Target changed?
                !hoveredImage.IsPointerOver
            ) // Pointer left?
            {
                fullBitmap?.Dispose(); // IMPORTANT: Dispose the unused bitmap
                return; // Don't show the flyout
            }

            // --- Show Flyout ---
            if (fullBitmap != null)
            {
                _previewImage.Source = fullBitmap; // Set loaded image

                // Store the target control *just before* showing
                // The Closed event handler uses this to detach
                _flyoutTargetControl = hoveredImage;

                // Show the flyout directly at the image control
                _previewFlyout.ShowAt(hoveredImage);
            }
            else
            {
                MessageDialog.ShowErrorDialog(
                    $"Failed to load/decode bitmap for preview: {item.Name}"
                );

                _previewImage.Source = null; // Clear image source
                // Clear target state if loading failed
                if (ReferenceEquals(_currentHoveredImage, hoveredImage))
                {
                    _currentHoveredImage = null;
                    _currentHoveredItem = null;
                }
            }
        }
        // catch (OperationCanceledException) // Catch cancellation if using CTS
        // {
        //     Console.WriteLine($"Preview load cancelled for {item.Name}."); // Debug
        //     fullBitmap?.Dispose(); // Dispose if cancelled mid-load
        // }
        catch (Exception ex)
        {
            MessageDialog.ShowErrorDialog(
                $"Error loading full image for preview ({item.ImagePath}): {ex.Message}"
            );

            _previewImage.Source = null; // Clear previous image on error
            // Clear target state on error
            if (ReferenceEquals(_currentHoveredImage, hoveredImage))
            {
                _currentHoveredImage = null;
                _currentHoveredItem = null;
            }
            fullBitmap?.Dispose(); // Ensure disposal on error
        }
    }

    /// <summary>
    /// Checks if a storage item represents a supported image file based on its name's extension.
    /// </summary>
    /// <param name="item">The storage item (file or folder).</param>
    /// <returns>True if the item is a file with a supported image extension, false otherwise.</returns>
    private bool IsImageFile(IStorageItem? item) // Changed parameter type
    {
        // Ensure it's a non-null IStorageFile
        if (item is not IStorageFile file)
        {
            return false; // It's null, a folder, or some other IStorageItem type
        }

        // Check the Name property for a valid extension
        if (string.IsNullOrEmpty(file.Name))
            return false;

        try
        {
            // Get the extension from the item's Name
            var extension = Path.GetExtension(file.Name);
            return !string.IsNullOrEmpty(extension)
                && SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
        catch (ArgumentException) // Path.GetExtension can throw on invalid chars
        {
            MessageDialog.ShowWarningDialog(
                $"Could not determine extension for file name: {file.Name}"
            );
            return false;
        }
    }

    /// <summary>
    /// Handles the DragOver event for the references ListBox.
    /// Checks if the dragged data contains valid image files using IStorageItem
    /// and sets DragEffects accordingly.
    /// </summary>
    private void HandleReferenceListDragOver(object? sender, DragEventArgs e)
    {
        // Default to no drop allowed
        e.DragEffects = DragDropEffects.None;

        // Use GetFiles() - it returns null if the format isn't present
        var storageItems = e.Data.GetFiles();

        // Check if we have any items and if *any* of them are valid image files
        if (storageItems?.Any(IsImageFile) ?? false) // Use the updated helper
        {
            // Allow copying the files
            e.DragEffects = DragDropEffects.Copy;
        }
        // Note: No explicit check for e.Data.Contains(DataFormats.Files) is strictly needed
        // because GetFiles() handles returning null if the format is absent.

        e.Handled = true; // We've decided whether to allow the drop or not
    }

    /// <summary>
    /// Handles the Drop event for the references ListBox.
    /// Processes dropped files asynchronously: copies valid images to the references folder,
    /// generates thumbnails, creates ReferencePaintingItem objects, and adds them to the collection.
    /// </summary>
    private async void HandleReferenceListDropAsync(object? sender, DragEventArgs e)
    {
        // Double-check if the data contains file names (DragOver should have ensured this)
        //if (!e.Data.Contains(DataFormats.FileNames))
        //{
        //    e.Handled = false; // Should not happen if DragOver worked correctly
        //    return;
        //}

        var filePaths = e.Data.GetFiles();
        if (filePaths?.Any() != true)
        {
            e.Handled = false;
            return;
        }

        // Indicate we are handling the drop with a Copy operation
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;

        //Console.WriteLine($"Drop detected with {filePaths.Count()} file(s).");

        // --- Process files asynchronously ---
        // Create a list to hold tasks for processing each file
        var processingTasks = new List<Task>();

        foreach (var storageFile in filePaths)
        {
            // Filter out non-image files before starting the task
            if (!IsImageFile(storageFile))
            {
                //Console.WriteLine($"Skipping non-image or invalid file: {storageFile}");
                continue;
            }

            // Add a task to process this specific file
            processingTasks.Add(
                Task.Run(async () =>
                {
                    string? filePath = storageFile.TryGetLocalPath();

                    if (string.IsNullOrEmpty(filePath))
                    {
                        // Log that we couldn't get a usable path and skip
                        MessageDialog.ShowWarningDialog(
                            $"Skipping file '{storageFile.Name}': Could not retrieve a local file system path."
                        );
                        return; // Stop processing this specific file
                    }

                    try
                    {
                        //Console.WriteLine($"Processing dropped file: {filePath}");

                        // 1. Copy file to the 'art/references' directory (using your existing method)
                        // This method already handles potential exceptions during copy
                        var newPath = CopyFileToArtFolder(filePath, REFERENCES_SUBFOLDER);
                        if (string.IsNullOrEmpty(newPath))
                        {
                            // Copy failed, error message should have been printed by CopyFileToArtFolder
                            return; // Stop processing this file
                        }

                        // 2. Generate thumbnail (using your existing async method)
                        var thumbnail = await LoadThumbnail(newPath, THUMBNAIL_SIZE);
                        if (thumbnail == null)
                        {
                            MessageDialog.ShowWarningDialog(
                                $"Warning: Failed to generate thumbnail for {newPath}"
                            );
                            // Continue without a thumbnail or decide how to handle this
                        }

                        // 3. Get display name (using your existing helper)
                        var displayName = GetDisplayNameFromFileName(Path.GetFileName(newPath));

                        // 4. Create ReferencePaintingItem
                        var newItem = new ReferencePaintingItem
                        {
                            Name = displayName,
                            ImagePath = newPath,
                            Thumbnail = thumbnail,
                        };

                        // 5. Add to collection *on the UI thread*
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _referencePaintings.Add(newItem);
                            //Console.WriteLine($"Added reference from drop: {newItem.Name}");
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageDialog.ShowErrorDialog(
                            $"Error processing dropped file '{storageFile}': {ex.Message}"
                        );
                    }
                })
            );
        }

        // --- Wait for all processing tasks to complete (optional) ---
        // You might want to await all tasks if you need to perform an action
        // after *all* files are processed. For adding items individually,
        // awaiting here isn't strictly necessary as InvokeAsync handles the UI updates.
        try
        {
            await Task.WhenAll(processingTasks);
            await Dispatcher.UIThread.InvokeAsync(SaveReferenceMetadataAsync);
            //Console.WriteLine("Finished processing all dropped files.");
        }
        catch (Exception ex)
        {
            MessageDialog.ShowWarningDialog(
                $"An error occurred while waiting for file processing tasks: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Gets the full path to the main art directory.
    /// </summary>
    private static string GetArtFolderPath() =>
        Path.Combine(Directory.GetCurrentDirectory(), ART_FOLDER_NAME);

    /// <summary>
    /// Gets the full path to the metadata file.
    /// </summary>
    private static string GetMetadataFilePath() =>
        Path.Combine(GetArtFolderPath(), METADATA_FILE_NAME);

    // --- IDisposable Implementation ---

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed data
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (e.g., event subscriptions if not handled by UIBuilder's auto-disposal)
                // Clear collections to release references
                _referencePaintings.Clear();
                _currentStudies.Clear();

                // If the root entity manages Avalonia controls directly (outside Flecs components),
                // ensure they are cleaned up if necessary. The Flecs OnRemove hooks should
                // handle disposal of components added via Set/Add.
                if (_root.IsValid() && _root.IsAlive())
                {
                    // If the Window/Control needs explicit cleanup not handled by Flecs hooks
                    // _root.Get<Grid>().Children.Clear(); // Example, if needed
                    // _root.Destruct(); // Let Flecs handle component disposal via hooks
                }
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
