using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS; // Assuming Module and UIBuilderExtensions are here or accessible
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage; // For FilePicker
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Flecs.NET.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Avalonia.Flecs.Controls.ECS.Module; // If Page tag is defined here


namespace Avalonia.Flecs.StellaLearning.Pages;

/*

We want to be able to easily right click on a refrence to copy it to the clip 
board so we can past it into our drawing program.

We should be able to open our refrence folder in the app, as well as other folders.

*/


// --- Data Structures (Placeholders - Adapt as needed) ---

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
    Other
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

        _root = _world.UI<Grid>(BuildArtPageUI)
                      .Add<Page>() // Add the Page tag
                      .Entity;
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
                .SetMargin(0, 0, 0, 10) // Margin below buttons
                .SetHorizontalAlignment(HorizontalAlignment.Left);

            buttonsPanel.Child<Button>(button =>
            {
                button.SetText("Add Reference...")
                      .OnClick(async (s, e) => await AddReferencePainting());
            });

            buttonsPanel.Child<Button>(button =>
            {
                // Disable initially until a reference is selected
                button.SetText("Add Study/Sketch...")
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
                .SetMargin(0, 0, 0, 5);
        });

        // --- ScrollViewer for the List (Row 2, takes remaining space *) ---
        grid.Child<ScrollViewer>(scrollViewer =>
        {
            scrollViewer.SetRow(2);

            // ListBox
            scrollViewer.Child<ListBox>(listBox =>
            {
                _referenceListBuilder = listBox; // Store builder reference


                var contextFlyout = _world.UI<MenuFlyout>((menuFlyout) =>
                                    {
                                        menuFlyout.OnOpened((sender, e) =>
                                        {
                                            if (!listBox.HasItemSelected())
                                            {
                                                menuFlyout.Hide();
                                            }
                                        });

                                        menuFlyout.Child<MenuItem>((item) =>
                                        {
                                            // Renames the list entry and its file name
                                            item.SetHeader("Rename")
                                            .OnClick((sender, _) =>
                                            {
                                                var item = listBox.GetSelectedItem<ReferencePaintingItem>();

                                                // Ensure we have the ListBox control instance
                                                if (listBox == null) return;

                                                // Get the item associated with the context menu click
                                                // The DataContext of the MenuItem should be the ReferencePaintingItem
                                                var itemToRename = (sender as Control)?.DataContext as ReferencePaintingItem ??
                                                                   listBox.GetSelectedItem<ReferencePaintingItem>(); // Fallback

                                                if (itemToRename == null) return;

                                                // Find the ListBoxItem container for positioning the flyout
                                                var container = listBox.Get<ListBox>().ContainerFromItem(itemToRename);
                                                if (container is Control targetControl)
                                                {
                                                    ShowRenameFlyout(targetControl, itemToRename);
                                                }
                                                else
                                                {
                                                    // Fallback: Show attached to the ListBox itself if container not found
                                                    ShowRenameFlyout(listBox.Get<ListBox>(), itemToRename);
                                                }

                                            });
                                        });

                                        menuFlyout.Child<MenuItem>((item) =>
                                        {
                                            //Removes the refrence from the list
                                            //Should not delete the file!
                                            item.SetHeader("Remove")
                                                .OnClick((_, _) => // Make the lambda async if you plan to add async dialogs
                                                {
                                                    if (listBox == null) return;

                                                    var itemToRemove = listBox.GetSelectedItem<ReferencePaintingItem>();
                                                    if (itemToRemove != null)
                                                    {
                                                        string filePath = itemToRemove.ImagePath;
                                                        string displayName = itemToRemove.Name; // For use in messages

                                                        // Basic validation
                                                        if (string.IsNullOrEmpty(filePath))
                                                        {
                                                            Console.WriteLine($"Error: ImagePath is missing for item '{displayName}'. Cannot delete.");
                                                            // TODO: Show error message to user via UI dialog/popup
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
                                                            Console.WriteLine($"Successfully deleted file: {filePath}");

                                                            // 2. If file deletion succeeded, THEN update UI and collection
                                                            // Clear details view if the deleted item was the selected one
                                                            if (ReferenceEquals(_selectedReference, itemToRemove))
                                                            {
                                                                _selectedReference = null;
                                                                _currentStudies.Clear();
                                                                // TODO: Consider disabling "Add Study" button state here
                                                            }

                                                            // Remove the item from the observable collection
                                                            _referencePaintings.Remove(itemToRemove);

                                                        }
                                                        catch (IOException ioEx) // Catch specific IO exceptions
                                                        {
                                                            Console.WriteLine($"Error deleting file '{filePath}' for item '{displayName}': {ioEx.Message}");
                                                            // TODO: Show specific error message to user (e.g., "File might be in use or path not found.")
                                                        }
                                                        catch (UnauthorizedAccessException authEx) // Catch permissions errors
                                                        {
                                                            Console.WriteLine($"Error deleting file '{filePath}' for item '{displayName}': {authEx.Message}");
                                                            // TODO: Show specific error message to user (e.g., "Permission denied.")
                                                        }
                                                        catch (Exception ex) // Catch any other unexpected errors
                                                        {
                                                            Console.WriteLine($"An unexpected error occurred while deleting file '{filePath}' for item '{displayName}': {ex.Message}");
                                                            // TODO: Show generic error message to user
                                                        }
                                                    }
                                                });
                                        });
                                    });
                listBox
                    .SetItemsSource(_referencePaintings)
                    .SetItemTemplate(CreateReferenceItemTemplate())
                    .SetSelectionMode(SelectionMode.Single)
                    .SetContextFlyout(contextFlyout)
                    .OnSelectionChanged(ReferenceList_SelectionChanged);
                // DockPanel fills remaining space by default
            });
        });
    }

    // --- Item Template Creation ---

    private FuncDataTemplate<ReferencePaintingItem> CreateReferenceItemTemplate()
    {
        return _world.CreateTemplate<ReferencePaintingItem, StackPanel>((builder, item) =>
        {
            // --- Attach Hover Handlers to the Template Root ---
            builder
                .SetOrientation(Orientation.Horizontal)
                .SetSpacing(5);
            // Attach event handlers:

            // --- End Hover Handlers Attachment ---

            if (item?.Thumbnail != null)
            {
                // Thumbnail Image
                builder.Child<Image>(img =>
                {
                    img
                       .SetBinding(Image.SourceProperty, nameof(ReferencePaintingItem.Thumbnail))
                       .SetWidth(THUMBNAIL_SIZE / 2.0) // Smaller thumbnail in list
                       .SetHeight(THUMBNAIL_SIZE / 2.0)
                       .SetStretch(Stretch.UniformToFill)
                       .SetVerticalAlignment(VerticalAlignment.Center)
                        //TODO: There is a big chance that a tooltip with a image attached 
                        // is much closer to the thing we want.
                        .OnPointerEntered(HandleItemPointerEntered)
                        .SetIsHitTestVisible(true);
                });

                // Name TextBlock
                builder.Child<TextBlock>(txt =>
                {
                    txt
                        .SetBinding(TextBlock.TextProperty, nameof(ReferencePaintingItem.Name))
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetIsHitTestVisible(false);
                });
            }
        });
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
            MinWidth = 150,        // Give it some initial width
            AcceptsReturn = false,   // Prevent multi-line names
        };

        // Create the Flyout to host the TextBox
        var renameFlyout = new Flyout
        {
            Content = renameTextBox,
            Placement = PlacementMode.BottomEdgeAlignedLeft, // Position it nicely below the item
            ShowMode = FlyoutShowMode.Transient // Hide when clicking outside
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
        Dispatcher.UIThread.Post(() =>
        {
            renameTextBox.Focus();
            renameTextBox.SelectAll(); // Select existing text for easy replacement
        }, DispatcherPriority.Input);
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
            Console.WriteLine($"Error: Could not parse path components for {oldPath}");
            // Consider showing an error message to the user
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
            Console.WriteLine("Rename skipped: New name is the same as the old name.");
            item.Name = newName; // Ensure ObservableObject updates UI even if file doesn't change case etc.
            return; // No file operation needed
        }

        if (File.Exists(newPath))
        {
            Console.WriteLine($"Error: Cannot rename. File already exists at {newPath}");
            // Show error to user (e.g., using a message box or status bar)
            // Example: await ShowErrorMessageAsync("Rename failed", $"A file named '{newFileName}' already exists.");
            return;
        }

        try
        {
            // Perform file move (rename) potentially off the UI thread
            await Task.Run(() => File.Move(oldPath, newPath));

            // --- Update item properties on the UI thread ---
            // Although Task.Run was used, this continuation might run on any thread.
            // It's safer to ensure UI property updates happen on the UI thread.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.ImagePath = newPath; // Update the stored path
                item.Name = newName;      // Update the observable property (UI will refresh)
                Console.WriteLine($"Successfully renamed '{Path.GetFileName(oldPath)}' to '{newFileName}'");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error renaming file from '{oldPath}' to '{newPath}': {ex.Message}");
            // Show error to user
            // Example: await ShowErrorMessageAsync("Rename failed", $"Could not rename the file: {ex.Message}");
            // No need to revert item properties here, as the file operation failed,
            // and the item properties were not yet updated.
        }
    }

    /// <summary>
    /// Creates the DataTemplate for the art studies ListBox.
    /// </summary>
    private IDataTemplate CreateStudyItemTemplate()
    {
        return _world.CreateTemplate<ArtStudyItem, StackPanel>((builder, item) =>
        {
            builder.SetOrientation(Orientation.Horizontal)
                   .SetSpacing(5);

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
                details.SetOrientation(Orientation.Vertical)
                       .SetVerticalAlignment(VerticalAlignment.Center);

                details.Child<TextBlock>(nameTxt => nameTxt.SetBinding(TextBlock.TextProperty, nameof(ReferencePaintingItem.Name)));
                details.Child<TextBlock>(typeTxt =>
                {
                    typeTxt.SetText(item.Type.ToString()) // Display study type
                           .SetFontSize(10)
                           .SetForeground(Brushes.Gray);
                });
            });
        });
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
            if (string.IsNullOrEmpty(newPath)) return; // Copy failed

            // 2. Create ReferencePaintingItem
            var newItem = new ReferencePaintingItem
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                ImagePath = newPath,
                // Generate thumbnail (implement LoadThumbnail)
                Thumbnail = await LoadThumbnail(newPath, THUMBNAIL_SIZE)
            };

            // 3. Add to collection (and potentially save metadata)
            _referencePaintings.Add(newItem);

            // 4. Optionally save metadata about references (e.g., to a JSON file or database)
            // SaveReferenceMetadata();
        }
    }

    private async Task AddArtStudy()
    {
        if (_selectedReference == null) return; // Should be disabled, but double-check

        Console.WriteLine($"Add Study/Sketch for: {_selectedReference.Name}");
        var filePath = await PickImageFileAsync("Select Study/Sketch Image");
        if (!string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine($"Selected file: {filePath}");
            // 1. Copy file to the 'art/studies' directory
            var newPath = CopyFileToArtFolder(filePath, STUDIES_SUBFOLDER);
            if (string.IsNullOrEmpty(newPath)) return; // Copy failed

            // 2. Determine Study Type (e.g., show a dialog)
            const StudyType studyType = StudyType.MasterCopy; // Placeholder

            // 3. Create ArtStudyItem
            var newItem = new ArtStudyItem
            {
                Name = Path.GetFileNameWithoutExtension(newPath),
                ImagePath = newPath,
                Type = studyType,
                Thumbnail = await LoadThumbnail(newPath, THUMBNAIL_SIZE)
            };

            // 4. Add to current studies collection (and save metadata)
            _currentStudies.Add(newItem);

            // 5. Save metadata about studies, linking them to references
            // SaveStudyMetadata();
        }
    }

    // --- File/Data Handling (Placeholders - Implement Robustly) ---

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
            Console.WriteLine($"Error creating art directories: {ex.Message}");
            // Handle error appropriately (e.g., show message to user)
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
            Console.WriteLine($"Error copying file to art folder: {ex.Message}");
            // Handle error (e.g., show message)
            return null;
        }
    }

    private static async Task<string?> PickImageFileAsync(string title)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll] // Use predefined image types
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
                if (!File.Exists(imagePath)) return null;

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
        if (underscoreIndex > 0 && // Ensure underscore is not the first character
            Guid.TryParse(nameWithoutExtension.Substring(0, underscoreIndex), out _))
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
    private void LoadReferencePaintings()
    {
        // Placeholder: Load reference painting metadata and populate _referencePaintings
        // Example: Read from a JSON file or database
        // For now, just scan the directory
        try
        {
            string baseArtPath = Path.Combine(Directory.GetCurrentDirectory(), ART_FOLDER_NAME);
            string referencesFolder = Path.Combine(baseArtPath, REFERENCES_SUBFOLDER);
            if (!Directory.Exists(referencesFolder)) return;

            _referencePaintings.Clear();
            var imageFiles = Directory.EnumerateFiles(referencesFolder, "*.*", SearchOption.TopDirectoryOnly)
                                      .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            foreach (var file in imageFiles)
            {
                // Load async
                Task.Run(async () =>
                {
                    // --- Modification Start ---
                    string fullPath = file;
                    string fileName = Path.GetFileName(fullPath);
                    string displayName = GetDisplayNameFromFileName(fileName); // Use the helper function
                    // --- Modification End ---

                    var newItem = new ReferencePaintingItem
                    {
                        Name = displayName, // <-- Use the parsed display name
                        ImagePath = fullPath, // <-- Keep the original full path
                        Thumbnail = await LoadThumbnail(fullPath, THUMBNAIL_SIZE)
                    };

                    // Use dispatcher to add to collection on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _referencePaintings.Add(newItem);
                    });
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading reference paintings: {ex.Message}");
        }
    }

    private void LoadStudiesForReference(ReferencePaintingItem reference)
    {
        // Placeholder: Load studies associated with the selected reference
        // This requires storing the association (e.g., in metadata)
        _currentStudies.Clear();
        Console.WriteLine($"Placeholder: Load studies for {reference.Name}");
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
            Margin = new Thickness(5)
        };
        _previewFlyout = new Flyout
        {
            Content = _previewImage,
            Placement = PlacementMode.RightEdgeAlignedTop,
            // Keep this mode, it handles dismissal nicely if pointer moves away
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
        };

        _previewFlyout.Closed += PreviewFlyout_Closed;
    }

    private void PreviewFlyout_Closed(object? sender, EventArgs e)
    {
        // When the flyout closes (for any reason), detach it from the control
        if (_flyoutTargetControl != null)
        {
            FlyoutBase.SetAttachedFlyout(_flyoutTargetControl, null);
            Console.WriteLine($"Flyout detached from {_flyoutTargetControl.GetType().Name}"); // Debug
            _flyoutTargetControl = null; // Clear the stored target
        }
        // Optional: Clear the image to free memory sooner
        if (_previewImage != null) _previewImage.Source = null;

        // Also clear hover state just in case Exited didn't fire correctly (e.g., window closed)
        _currentHoveredImage = null;
        _currentHoveredItem = null;
        // _previewLoadCts?.Cancel(); // Ensure cancellation on close if using CTS
    }

    private async void HandleItemPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Image hoveredImage || hoveredImage.DataContext is not ReferencePaintingItem item || _previewFlyout == null || _previewImage == null)
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
        _currentHoveredItem = item;         // Set new target item
                                            // _previewLoadCts?.Cancel(); // Cancel previous load if using CTS
                                            // _previewLoadCts = new CancellationTokenSource(); // Create new token if using CTS
                                            // var cancellationToken = _previewLoadCts.Token; // Get token if using CTS

        Console.WriteLine($"Pointer Entered Image for: {item.Name}. Loading preview..."); // Debug

        Bitmap? fullBitmap = null;
        try
        {
            // Load the image asynchronously off the UI thread
            fullBitmap = await Task.Run(async () => // Make inner lambda async if needed
            {
                // cancellationToken.ThrowIfCancellationRequested(); // Check cancellation if using CTS
                if (!File.Exists(item.ImagePath)) return null;
                try
                {
                    await using var stream = File.OpenRead(item.ImagePath);
                    // Decode reasonably large but capped
                    // Consider adding cancellationToken support to async file/decode methods if possible
                    return Bitmap.DecodeToWidth(stream, PREVIEW_MAX_SIZE * 2);
                }
                catch (Exception decodeEx)
                {
                    Console.WriteLine($"Error DECODING image for preview ({item.ImagePath}): {decodeEx.Message}");
                    return null;
                }
            }/*, cancellationToken*/); // Pass token if using CTS

            // --- Post-Load Check ---
            // ThrowIfCancellationRequested(); // Check cancellation if using CTS

            // Check if we are STILL hovering over the SAME image after the async load completed
            if (!ReferenceEquals(_currentHoveredImage, hoveredImage) || // Target changed?
                !hoveredImage.IsPointerOver)                          // Pointer left?
            {
                Console.WriteLine($"Preview loaded for {item.Name}, but hover target changed/left during load."); // Debug
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
                Console.WriteLine($"Showing preview flyout for {item.Name}"); // Debug
            }
            else
            {
                // Handle load/decode failure
                Console.WriteLine($"Failed to load/decode bitmap for preview: {item.Name}"); // Debug
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
            // Catch potential errors during Task.Run setup or file access before stream creation
            Console.WriteLine($"Error loading full image for preview ({item.ImagePath}): {ex.Message}");
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
