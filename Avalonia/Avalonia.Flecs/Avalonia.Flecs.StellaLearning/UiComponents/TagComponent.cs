using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Flecs.NET.Core;
using System.Reactive.Disposables;
using NLog; // Assuming you use NLog like in ComparePriority

namespace Avalonia.Flecs.StellaLearning.UiComponents // Adjust namespace if needed
{
    /// <summary>
    /// A reusable UI component for managing a list of tags.
    /// Allows adding tags via text input and removing existing tags.
    /// </summary>
    public class TagComponent : IUIComponent, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger(); // Optional logging
        private readonly CompositeDisposable _disposables = [];
        private bool _isDisposed = false;

        private readonly Entity _root;
        private readonly World _world; // Keep world reference for creating templates

        // --- UI Element Builders (optional, but good for reference) ---
        private UIBuilder<TextBox>? _tagInputTextBox;
        private UIBuilder<Button>? _addTagButton;
        private UIBuilder<ItemsControl>? _tagsItemsControl;
        // ---

        /// <summary>
        /// The underlying collection of tags managed by this component.
        /// Changes to this collection will reflect in the UI.
        /// </summary>
        public ObservableCollection<string> Tags { get; } = [];

        /// <inheritdoc/>
        public Entity Root => _root;

        /// <summary>
        /// Creates a new Tag Management UI Component.
        /// </summary>
        /// <param name="world">The Flecs world.</param>
        public TagComponent(World world)
        {
            _world = world; // Store world for template creation

            // Define the ItemTemplate for displaying each tag
            var tagItemTemplate = world.CreateTemplate<string, Border>((borderBuilder, tagText) =>
            {
                borderBuilder
                    .SetMargin(2)
                    .SetPadding(5, 2)
                    .SetCornerRadius(new CornerRadius(4))
                    .SetBackground(Brushes.LightGray) // Example background
                    .Child<StackPanel>(stackPanel =>
                    {
                        stackPanel
                            .SetOrientation(Orientation.Horizontal)
                            .SetSpacing(5)
                            .SetVerticalAlignment(VerticalAlignment.Center);

                        // Tag Text
                        stackPanel.Child<TextBlock>(textBlock =>
                        {
                            textBlock
                                .SetText(tagText)
                                .SetVerticalAlignment(VerticalAlignment.Center);
                        });

                        // Remove Button ('x')
                        stackPanel.Child<Button>(removeButton =>
                        {
                            removeButton
                                .SetText("x")
                                .SetPadding(2, 0)
                                .SetFontSize(10)
                                .SetVerticalAlignment(VerticalAlignment.Center)
                                .SetHorizontalAlignment(HorizontalAlignment.Center)
                                // Optional: Style the remove button further (e.g., transparent background, specific foreground)
                                //.SetBackground(Brushes.Transparent)
                                //.SetForeground(Brushes.DarkRed)
                                .SetBorderThickness(new Thickness(0))
                                .OnClick((_, _) =>
                                {
                                    Tags.Remove(tagText);
                                });
                        });
                    });
            });

            // --- Build the main component UI ---
            _root = world.UI<StackPanel>(rootPanel =>
            {
                rootPanel
                    .SetOrientation(Orientation.Vertical)
                    .SetSpacing(5);

                // --- Input Area (TextBox + Add Button) ---
                rootPanel.Child<Grid>(inputGrid =>
                {
                    inputGrid
                        .SetColumnDefinitions("*,Auto") // TextBox takes available space, Button takes needed space
                        .SetRowDefinitions("Auto");

                    // Tag Input TextBox
                    inputGrid.Child<TextBox>(textBox =>
                    {
                        _tagInputTextBox = textBox; // Store reference
                        textBox
                            .SetWatermark("Add Tag...")
                            .SetColumn(0)
                            .OnKeyDown((sender, args) =>
                            {
                                if (args.Key == Key.Enter)
                                {
                                    AddTagFromInput();
                                    args.Handled = true; // Prevent further processing of Enter key
                                }
                            });
                        // Optionally subscribe to TextChanged for real-time validation/suggestions
                    });

                    // Add Tag Button
                    inputGrid.Child<Button>(button =>
                    {
                        _addTagButton = button; // Store reference
                        button
                            .SetText("Add")
                            .SetColumn(1)
                            .SetMargin(5, 0, 0, 0) // Add some space between textbox and button
                            .OnClick((_, _) =>
                            {
                                AddTagFromInput();
                            });
                    });
                });

                // --- Tag Display Area ---
                rootPanel.Child<ItemsControl>(itemsControl =>
                {
                    _tagsItemsControl = itemsControl; // Store reference
                    itemsControl
                        .SetItemsSource(Tags) // Bind to the ObservableCollection
                        .SetItemTemplate(tagItemTemplate) // Use the template defined above
                        .With(ic => // Use 'With' for properties not covered by extensions
                        {
                            // Use a WrapPanel so tags flow to the next line
                            ic.ItemsPanel = new FuncTemplate<Panel>(() => new WrapPanel())!;
                        });
                    // Optional: Add styling like a border or background
                    // .SetBorderBrush(Brushes.Gray)
                    // .SetBorderThickness(new Thickness(1))
                    // .SetMinHeight(50) // Ensure it has some visible space even when empty
                });
            });

            // Add the root entity destruction to the disposables
            _disposables.Add(Disposable.Create(() =>
            {
                if (_root.IsValid() && _root.IsAlive())
                {
                    Logger.Debug($"Disposing TagComponent Root Entity: {_root.Id}");
                    _root.Destruct(); // Destroy the entire UI entity hierarchy for this component
                }
            }));

            // Add event handler cleanup to disposables if needed (OnClick is usually managed by Avalonia's weak event manager, but explicit cleanup is safer if unsure)
            // Example: _disposables.Add(Disposable.Create(() => { if (_addTagButton != null) { /* Unsubscribe logic if needed */ } }));
            // OnKeyDown might need explicit detachment if added directly to the control instance outside the builder actions. Since it's done via the builder extension, it *should* be okay, but explicit cleanup is safest.

            _root.SetName($"TAGCOMPONENT-{new Random().Next()}"); // Give it a debug name
            Logger.Info($"TagComponent created with Root Entity: {_root.Id}");
        }

        /// <summary>
        /// Helper method to add a tag from the input TextBox.
        /// </summary>
        private void AddTagFromInput()
        {
            if (_tagInputTextBox == null) return;

            string newTag = _tagInputTextBox.GetText()?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(newTag) && !Tags.Contains(newTag))
            {
                Tags.Add(newTag);
                _tagInputTextBox.SetText(string.Empty); // Clear input after adding
                Logger.Debug($"Tag added: {newTag}");
            }
            else if (string.IsNullOrWhiteSpace(newTag))
            {
                Logger.Debug("Attempted to add empty tag.");
                // Optionally provide user feedback (e.g., shake the textbox, show a message)
            }
            else if (Tags.Contains(newTag))
            {
                Logger.Debug($"Attempted to add duplicate tag: {newTag}");
                // Optionally provide user feedback
                _tagInputTextBox.SetText(string.Empty); // Clear input even if duplicate
            }
        }

        /// <summary>
        /// Clears all tags from the component.
        /// </summary>
        public void ClearTags()
        {
            Tags.Clear();
            Logger.Debug("All tags cleared.");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Logger.Debug($"Disposing TagComponent (Root: {_root.Id})...");
                    // Dispose managed state (managed objects).
                    _disposables.Dispose(); // This handles root entity destruction and event cleanup if added.
                    Tags.Clear(); // Clear the collection as the UI is gone.
                    Logger.Debug($"TagComponent disposed (Root: {_root.Id}).");
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // (Not applicable in this specific component, but standard pattern)

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer (Destructor)
        /// </summary>
        ~TagComponent()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
    }
}