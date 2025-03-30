using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Clowd.Clipboard;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using Path = System.IO.Path;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// An image cloze is an item where you hide specific text or part of the image to be
/// filled with a word. Often you can see this done for biological or anatomy topics
/// done. But this technique works great for any field where diagrams or images
/// can be used to explain something.
/// </summary>
public class EditImageCloze : IUIComponent, IDisposable
{
    private class ClozeArea
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; } = string.Empty;

        public IBrush FillColor { get; set; } = new SolidColorBrush(Color.FromArgb(
                    a: 180,
                    r: 221,
                    g: 176,
                    b: 55));

    }

    /// <summary>
    /// Collection to track all disposables
    /// </summary>
    private readonly CompositeDisposable _disposables = [];
    private UIBuilder<Button>? createButton = null;
    private UIBuilder<TextBox>? nameTextBox = null;
    private UIBuilder<Image>? image = null;
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private readonly ObservableCollection<string> clozes = [];
    private bool isDisposed = false;
    private EventHandler<RoutedEventArgs>? createButtonClickedHandler;

    private Point dragStartPoint;
    private bool isDragging = false;
    private Entity selectionRectangle;
    private Entity canvasEntity;
    private readonly ObservableCollection<ClozeArea> clozeAreas = [];

    // Replace the simple string field with a BehaviorSubject
    private readonly BehaviorSubject<string> _imagePathSubject = new(string.Empty);
    // Property to access the current value
    private string ImagePath => _imagePathSubject.Value;
    private SpacedRepetitionImageCloze _spacedRepetitionImageCloze;
    /// <summary>
    /// Create the Add Cloze Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="spacedRepetitionImageCloze"></param>
    /// <returns></returns>
    public EditImageCloze(World world, SpacedRepetitionImageCloze spacedRepetitionImageCloze)
    {
        _spacedRepetitionImageCloze = spacedRepetitionImageCloze;

        _disposables.Add(_imagePathSubject
            .Skip(1) // Skip the initial empty value
            .Subscribe(path =>
            {
                ImagePathChanged();
            }));


        // Convert List<ImageClozeArea> to ObservableCollection<ClozeArea>
        foreach (var area in _spacedRepetitionImageCloze.ClozeAreas)
        {
            clozeAreas.Add(new ClozeArea
            {
                X = area.X,
                Y = area.Y,
                Width = area.Width,
                Height = area.Height,
                Text = area.Text
            });
        }

        _root = world.UI<Window>((window) =>
        {
            window
            .OnKeyDown(Window_KeyDown)
            .SetTitle($"Edit Image Cloze: {_spacedRepetitionImageCloze.Name}")
            .SetWidth(400)
            .SetHeight(400)
            .Child<ScrollViewer>((scrollViewer) =>
            {
                scrollViewer
                .SetRow(1)
                .SetColumnSpan(3)
                .Child(DefineWindowContents(world));
            });

            window.OnClosed((sender, args) => Dispose());
            window.Show();
        });
        // We want to change the image path AFTER the ui is constructed.
        // Because the handler expects ui elements to exit.
        _imagePathSubject.OnNext(_spacedRepetitionImageCloze.ImagePath);
        DrawExistingClozeAreas();
    }

    private Entity DefineWindowContents(World world)
    {
        return world.UI<StackPanel>((stackPanel) =>
        {

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBox>((textBox) =>
            {
                nameTextBox = textBox;
                textBox.SetWatermark("Name")
                .SetText(_spacedRepetitionImageCloze.Name);
            });

            stackPanel.Child(world.UI<Button>((button) =>
                                {
                                    button.Child<TextBlock>((t) => t.SetText("Select Image"));
                                    button.OnClick(async (e, args) =>
                                    {
                                        var path = await FilePickerAsync();
                                        _imagePathSubject.OnNext(path); // Update using OnNext instead of assignment
                                    });
                                }));

            stackPanel.Child<Grid>((grid) =>
            {

                grid.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                grid.SetVerticalAlignment(VerticalAlignment.Stretch);
                grid.SetMinHeight(250); // Set a reasonable minimum height

                // Add a Viewbox to contain and scale the image properly
                grid.Child<Viewbox>((viewbox) =>
                {
                    //viewbox.SetStretch(Stretch.Uniform);
                    viewbox.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    viewbox.SetVerticalAlignment(VerticalAlignment.Stretch);

                    // Add a Canvas inside the Viewbox for positioning elements
                    viewbox.Child<Canvas>((canvas) =>
                    {
                        canvasEntity = canvas.Entity;

                        //viewbox.With((w) => { w.Child = canvas.Get<Canvas>(); });
                        // Add the image to the canvas
                        canvas.Child<Image>((image) =>
                        {
                            this.image = image;

                            if (ImagePath.Length != 0)
                            {
                                try
                                {
                                    var bitmap = new Bitmap(File.OpenRead(ImagePath));
                                    image.SetSource(bitmap);

                                    // Set the canvas size to match the image's natural size
                                    canvas.SetWidth(bitmap.Size.Width);
                                    canvas.SetHeight(bitmap.Size.Height);
                                }
                                catch (FileNotFoundException ex)
                                {
                                    var cd = new ContentDialog()
                                    {
                                        Title = "Picture not found",
                                        Content = $"The picture couldn't not be found at path: {ex.FileName}",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                }
                            }

                            image
                            .OnPointerMoved(Image_PointerMoved)
                            .OnPointerPressed(Image_PointerPressed)
                            .OnPointerReleased(Image_PointerReleased);

                            _disposables.Add(Disposable.Create(() =>
                            {
                                if (image.Entity.IsValid())
                                {
                                    image
                                    .RemoveOnPointerMoved(Image_PointerMoved)
                                    .RemoveOnPointerPressed(Image_PointerPressed)
                                    .RemoveOnPointerReleased(Image_PointerReleased);
                                }
                            }));
                        });
                    });
                });
            });

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock.SetTextWrapping(TextWrapping.Wrap);
                textBlock.SetText("Mark a area in the image to create a cloze for it. To delete it again simply right click and select remove");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
            });

            stackPanel.Child<Separator>((separator) =>
            {
                separator
                    .SetMargin(0, 0, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
            });


            var tagManager = new TagComponent(world, _spacedRepetitionImageCloze.Tags);
            stackPanel.Child(tagManager); // Add the tag manager UI

            // Create button
            stackPanel.Child<Button>((button) =>
            {
                createButton = button;
                button
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch);
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Save Changes");
                });


                createButtonClickedHandler = (sender, args) =>
                {
                    if (nameTextBox is null)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(nameTextBox.GetText()) || string.IsNullOrEmpty(ImagePath))
                    {
                        nameTextBox!.SetWatermark("Name");
                        return;
                    }

                    if (clozeAreas.Count == 0)
                    {
                        var cd = new ContentDialog()
                        {
                            Title = "Missing Areas",
                            Content = "Your image does not currently have hidding any areas defined, they are required",
                            PrimaryButtonText = "Ok",
                            DefaultButton = ContentDialogButton.Primary,
                            IsSecondaryButtonEnabled = true,
                        };
                        cd.ShowAsync();
                        return;
                    }

                    if (_root.IsValid())
                    {
                        // Create a list of ImageClozeArea objects from our clozeAreas collection
                        var imageClozeAreas = clozeAreas.Select(area => new ImageClozeArea
                        {
                            X = area.X,
                            Y = area.Y,
                            Width = area.Width,
                            Height = area.Height,
                            Text = area.Text
                        }).ToList();


                        _spacedRepetitionImageCloze.Name = nameTextBox.GetText();
                        _spacedRepetitionImageCloze.ImagePath = ImagePath;
                        _spacedRepetitionImageCloze.ClozeAreas = imageClozeAreas;
                        _spacedRepetitionImageCloze.Tags = [.. tagManager.Tags];

                        _root.Get<Window>().Close();
                    }
                };

                button.With((b) => b.Click += createButtonClickedHandler);
            });
        });
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; 
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {

                if (createButton is not null && createButtonClickedHandler is not null)
                {
                    createButton.With((b) => b.Click -= createButtonClickedHandler);
                }

                // Clean up other resources
                // Consider calling destruct if needed
                if (_root.IsValid())
                {
                    _root.Get<Window>().Content = null;
                    _root.Destruct();
                }

                // Dispose all tracked disposables
                _disposables.Dispose();
            }

            isDisposed = true;
        }
    }

    private void Image_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        // Start dragging
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            dragStartPoint = e.GetPosition(canvasEntity.Get<Canvas>());
            isDragging = true;

            selectionRectangle = canvasEntity.CsWorld().UI<Rectangle>((rect) =>
            {
                rect
                .SetStroke(new SolidColorBrush(Colors.Red))
                .SetStrokeThickness(2)
                .SetFill(new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)))
                .SetWidth(0)
                .SetHeight(0);
            });

            canvasEntity.Get<Canvas>().Children.Add(selectionRectangle.Get<Rectangle>());
        }
    }

    private void ImagePathChanged()
    {
        if (ImagePath.Length != 0)
        {
            try
            {
                var bitmap = new Bitmap(File.OpenRead(ImagePath));

                image?.With((img) => img.Source = bitmap);

                var canvas = canvasEntity.Get<Canvas>();

                // Set the canvas size to match the image's natural size
                canvas.Width = bitmap.Size.Width;
                canvas.Height = bitmap.Size.Height;
            }
            catch (FileNotFoundException ex)
            {
                var cd = new ContentDialog()
                {
                    Title = "Picture not found",
                    Content = $"The picture couldn't not be found at path: {ex.FileName}",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            }
        }
    }

    private void Image_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!isDragging || !selectionRectangle.IsValid())
            return;

        var currentPoint = e.GetPosition(canvasEntity.Get<Canvas>());

        // Calculate rectangle dimensions
        double left = Math.Min(dragStartPoint.X, currentPoint.X);
        double top = Math.Min(dragStartPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - dragStartPoint.X);
        double height = Math.Abs(currentPoint.Y - dragStartPoint.Y);

        // Update rectangle position and size
        var rect = selectionRectangle.Get<Rectangle>();
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        rect.Width = width;
        rect.Height = height;
    }

    private void Image_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (!isDragging || !selectionRectangle.IsValid())
            return;

        isDragging = false;

        var rect = selectionRectangle.Get<Rectangle>();
        double left = Canvas.GetLeft(rect);
        double top = Canvas.GetTop(rect);
        double width = rect.Width;
        double height = rect.Height;
        var rectColor = new SolidColorBrush(Color.FromArgb(
            a: 180,
            r: 221,
            g: 176,
            b: 55));

        // Only create a cloze area if it has a reasonable size
        if (width > 10 && height > 10)
        {
            // Create a TextBox at the selection position
            var world = canvasEntity.CsWorld();
            var rectangleEntity = world.UI<Rectangle>((rect) =>
            {


                rect
                .SetWidth(width)
                .SetHeight(height)
                .SetFill(rectColor);

                //textBox.SetBackground(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)));

                var menu = world.UI<MenuFlyout>((menu) =>
                    {
                        menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                        menu.Child<MenuItem>((menuItem) =>
                        {
                            menuItem
                            .SetHeader("Remove")
                            .OnClick((_, _) =>
                            {
                                canvasEntity.Get<Canvas>().Children.Remove(rect.Get<Rectangle>());

                                // Remove the corresponding cloze area
                                var clozeToRemove = clozeAreas.FirstOrDefault(ca =>
                                    Math.Abs(ca.X - Canvas.GetLeft(rect.Get<Rectangle>())) < 0.1 &&
                                    Math.Abs(ca.Y - Canvas.GetTop(rect.Get<Rectangle>())) < 0.1 &&
                                    Math.Abs(ca.Width - rect.Get<Rectangle>().Width) < 0.1 &&
                                    Math.Abs(ca.Height - rect.Get<Rectangle>().Height) < 0.1);

                                if (clozeToRemove != null)
                                {
                                    clozeAreas.Remove(clozeToRemove);
                                }

                                // Destroy the entity
                                rect.Entity.Destruct();
                            });
                        });
                    });

                rect.SetContextFlyout(menu);
            });

            var rectangle = rectangleEntity.Get<Rectangle>();
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            canvasEntity.Get<Canvas>().Children.Add(rectangle);

            // Store the cloze area
            var clozeArea = new ClozeArea
            {
                X = left,
                Y = top,
                Width = width,
                Height = height,
                FillColor = rectColor,
                Text = ""
            };


            clozeAreas.Add(clozeArea);
        }

        // Remove the selection rectangle
        canvasEntity.Get<Canvas>().Children.Remove(rect);
        selectionRectangle.Destruct();
    }

    private static async Task<string> FilePickerAsync()
    {
        // Create and configure the file picker options
        var options = new FilePickerOpenOptions()
        {
            Title = "Select an image",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
            FileTypeFilter = [CustomFilePickerTypes.ImageFileType]
        };

        // Create an OpenFileDialog instance
        IReadOnlyList<IStorageFile> result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

        if (result?.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            return file.TryGetLocalPath()!;
        }
        return string.Empty;
    }

    // Add this method to handle keyboard events
    private async void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // Check for Ctrl+V
        if (e.Key == Avalonia.Input.Key.V && e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
        {
            await PasteImageFromClipboard();
        }
    }

    // Add this method to handle pasting from clipboard
    private async Task PasteImageFromClipboard()
    {
        var clipboard = TopLevel.GetTopLevel(_root.Get<Window>())?.Clipboard;
        if (clipboard == null)
            return;

        // This functionality only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // Get clipboard content
        var data = ClipboardAvalonia.GetImage();

        if (data is Bitmap bitmap)
        {
            try
            {
                // Create a temp file for the image
                string tempPath = Path.Combine(Path.GetTempPath(), $"cloze_image_{Guid.NewGuid()}.png");

                // Save the bitmap to the temp file
                await using (var fs = File.OpenWrite(tempPath))
                {
                    bitmap.Save(fs);
                }

                // Update the image path
                _imagePathSubject.OnNext(tempPath);

                // Show notification or update UI to indicate successful paste
                if (nameTextBox != null && string.IsNullOrEmpty(nameTextBox.GetText()))
                {
                    nameTextBox.SetText("Pasted Image");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                Console.WriteLine($"Error pasting image: {ex.Message}");
            }
        }
    }

    private void DrawExistingClozeAreas()
    {
        if (!canvasEntity.IsValid())
            return;

        var canvas = canvasEntity.Get<Canvas>();
        var world = canvasEntity.CsWorld();

        foreach (var clozeArea in clozeAreas)
        {
            var rectangleEntity = world.UI<Rectangle>((rect) =>
            {
                rect
                .SetWidth(clozeArea.Width)
                .SetHeight(clozeArea.Height)
                .SetFill(clozeArea.FillColor);

                var menu = world.UI<MenuFlyout>((menu) =>
                {
                    menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Remove")
                        .OnClick((_, _) =>
                        {
                            canvas.Children.Remove(rect.Get<Rectangle>());

                            // Remove the corresponding cloze area
                            clozeAreas.Remove(clozeArea);

                            // Destroy the entity
                            rect.Entity.Destruct();
                        });
                    });
                });

                rect.SetContextFlyout(menu);
            });

            var rectangle = rectangleEntity.Get<Rectangle>();
            Canvas.SetLeft(rectangle, clozeArea.X);
            Canvas.SetTop(rectangle, clozeArea.Y);
            canvas.Children.Add(rectangle);
        }
    }
}