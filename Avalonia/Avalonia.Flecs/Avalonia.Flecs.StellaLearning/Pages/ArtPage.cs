using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS; // Assuming Module and UIBuilderExtensions are here or accessible
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage; // For FilePicker
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
    private UIBuilder<Image>? _selectedImageBuilder;
    private UIBuilder<TextBlock>? _selectedImageNameBuilder;
    private UIBuilder<ListBox>? _studyListBuilder;

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
        grid.SetRowDefinitions("Auto, *") // Row 0 for buttons, Row 1 for content
            .SetColumnDefinitions("Auto, *"); // Col 0 for reference list, Col 1 for details

        // --- Row 0: Action Buttons ---
        grid.Child<StackPanel>(buttonsPanel =>
        {
            buttonsPanel
                .SetOrientation(Orientation.Horizontal)
                .SetSpacing(10)
                .SetMargin(0, 0, 0, 10) // Margin below buttons
                .SetRow(0)
                .SetColumn(0)
                .SetColumnSpan(2) // Span across both columns
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

        // --- Row 1, Column 0: Reference Paintings List ---
        grid.Child<Border>(listBorder => // Add border for visual structure
        {
            listBorder
               .SetRow(1)
               .SetColumn(0)
               .SetBorderBrush(Brushes.Gray)
               .SetBorderThickness(new Thickness(0, 0, 1, 0)) // Right border
               .SetPadding(0, 0, 10, 0); // Padding to the right

            listBorder.Child<DockPanel>(dockPanel =>
            {
                // Title
                dockPanel.Child<TextBlock>(title =>
                {
                    title.SetText("References")
                         .SetFontWeight(FontWeight.Bold)
                         .SetMargin(0, 0, 0, 5)
                         .SetDock(Dock.Top);
                });

                // ListBox
                dockPanel.Child<ListBox>(listBox =>
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
                                                .OnClick((_, _) =>
                                                {
                                                    var item = listBox.GetSelectedItem<ReferencePaintingItem>();
                                                    item.Name = "CHANGED";
                                                });
                                            });

                                            menuFlyout.Child<MenuItem>((item) =>
                                            {
                                                //Removes the refrence from the list
                                                //Should not delete the file!
                                                item.SetHeader("Remove")
                                                .OnClick((_, _) =>
                                                {
                                                    ClearSelectedReferenceView();
                                                    var item = listBox.GetSelectedItem<ReferencePaintingItem>();
                                                    _selectedReference = null;
                                                    _referencePaintings.Remove(item);
                                                    _currentStudies.Clear();
                                                });

                                            });
                                        });
                    listBox
                        .SetItemsSource(_referencePaintings)
                        .SetItemTemplate(CreateReferenceItemTemplate())
                        .SetMinWidth(200) // Ensure minimum width
                        .SetSelectionMode(SelectionMode.Single)
                        .SetContextFlyout(contextFlyout)
                        .OnSelectionChanged(ReferenceList_SelectionChanged);
                    // DockPanel fills remaining space by default
                });
            });
        });


        // --- Row 1, Column 1: Selected Item Details ---
        grid.Child<Grid>(detailsGrid =>
        {
            detailsGrid
                .SetRow(1)
                .SetColumn(1)
                .SetMargin(10, 0, 0, 0) // Left margin
                .SetRowDefinitions("Auto, *, Auto, Auto"); // Name, Image, Studies Title, Studies List

            // Selected Reference Name
            detailsGrid.Child<TextBlock>(nameBlock =>
            {
                _selectedImageNameBuilder = nameBlock;
                nameBlock
                    .SetRow(0)
                    .SetText("Select a reference")
                    .SetFontSize(16)
                    .SetFontWeight(FontWeight.Bold)
                    .SetMargin(0, 0, 0, 10);
            });

            // Selected Reference Image (inside a ScrollViewer)
            detailsGrid.Child<ScrollViewer>(imageScroll =>
            {
                imageScroll.SetRow(1)
                           .SetHorizontalScrollBarVisibility(ScrollBarVisibility.Auto)
                           .SetVerticalScrollBarVisibility(ScrollBarVisibility.Auto);

                imageScroll.Child<Image>(image =>
                {
                    _selectedImageBuilder = image; // Store builder reference
                    image
                        .SetStretch(Stretch.Uniform) // Scale image nicely
                        .SetHorizontalAlignment(HorizontalAlignment.Center)
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetMinWidth(300) // Min size for the image area
                        .SetMinHeight(300);
                    // Add placeholder background?
                    // .SetBackground(Brushes.LightGray);
                });
            });


            // Studies Title
            detailsGrid.Child<TextBlock>(studiesTitle =>
            {
                studiesTitle
                    .SetRow(2)
                    .SetText("Studies / Sketches")
                    .SetFontWeight(FontWeight.Bold)
                    .SetMargin(0, 10, 0, 5); // Margin top and bottom
            });

            // Associated Studies List
            detailsGrid.Child<ListBox>(studyList =>
            {
                _studyListBuilder = studyList; // Store builder reference
                studyList
                    .SetRow(3)
                    .SetItemsSource(_currentStudies)
                    .SetItemTemplate(CreateStudyItemTemplate())
                    .SetMinHeight(150); // Min height for study list
                                        // Add selection handling if needed (e.g., view study larger)
                                        // .OnSelectionChanged(...)
            });
        });
    }

    // --- Item Template Creation ---

    /// <summary>
    /// Creates the DataTemplate for the reference paintings ListBox.
    /// </summary>
    private FuncDataTemplate<ReferencePaintingItem> CreateReferenceItemTemplate()
    {
        // Using the UIBuilder extension method for templates
        return _world.CreateTemplate<ReferencePaintingItem, StackPanel>((builder, item) =>
        {
            builder.SetOrientation(Orientation.Horizontal)
                   .SetSpacing(5);

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
                       .SetVerticalAlignment(VerticalAlignment.Center);
                });

                // Name TextBlock
                builder.Child<TextBlock>(txt =>
                {
                    txt
                        .SetBinding(TextBlock.TextProperty, nameof(ReferencePaintingItem.Name))
                        .SetVerticalAlignment(VerticalAlignment.Center);
                });
            }
        });
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
            UpdateSelectedReferenceView();
            LoadStudiesForReference(selectedItem); // Implement this

            // Enable "Add Study" button (Requires access to the button entity/builder)
            // Find the button entity and enable it, e.g.:
            // var addButtonEntity = _root.FindEntityByName("AddStudyButton"); // Need a way to name/find entities
            // addButtonEntity?.Enable<Button>(); // Assuming Enable extension exists
        }
        else
        {
            _selectedReference = null;
            ClearSelectedReferenceView();
            _currentStudies.Clear();

            // Disable "Add Study" button
            // Find the button entity and disable it
            // var addButtonEntity = _root.FindEntityByName("AddStudyButton");
            // addButtonEntity?.Disable<Button>();
        }
    }

    private void UpdateSelectedReferenceView()
    {
        if (_selectedReference != null && _selectedImageBuilder != null && _selectedImageNameBuilder != null)
        {
            try
            {
                // Load the full image
                if (File.Exists(_selectedReference.ImagePath))
                {
                    var bitmap = new Bitmap(_selectedReference.ImagePath);
                    _selectedImageBuilder.SetSource(bitmap);
                }
                else
                {
                    _selectedImageBuilder.RemoveSource(); // Or set a "not found" image
                }
                _selectedImageNameBuilder.SetText(_selectedReference.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading selected image: {ex.Message}");
                _selectedImageBuilder?.RemoveSource(); // Clear on error
                _selectedImageNameBuilder?.SetText($"Error loading: {_selectedReference.Name}");
            }
        }
        else
        {
            ClearSelectedReferenceView();
        }
    }

    private void ClearSelectedReferenceView()
    {
        _selectedImageBuilder?.RemoveSource();
        _selectedImageNameBuilder?.SetText("Select a reference");
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
                Name = Path.GetFileNameWithoutExtension(newPath),
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
            StudyType studyType = StudyType.MasterCopy; // Placeholder

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

    private void EnsureArtDirectories()
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

    private string? CopyFileToArtFolder(string sourcePath, string subfolder)
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

    private async Task<string?> PickImageFileAsync(string title)
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

    private async Task<Bitmap?> LoadThumbnail(string imagePath, int size)
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
                    var newItem = new ReferencePaintingItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        ImagePath = file,
                        Thumbnail = await LoadThumbnail(file, THUMBNAIL_SIZE)
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
