using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;

/*
TODO: Highly Experimental, the goal of this module is to improve the way
we create a UI in code. Its all about making it more obvious how the UI is 
structured. We take insperation how its done in Flutter.
*/

/*
Also this UI Builder class should become a really deep class with a rather small interface.
One way we reduce the interface is by putting many methods related to specific avalonia classes
to generic with their type. So they are only exposed when working with the type itself. This 
shows information where its needed and hides it where it doesnt.
*/

// DESING HINT: These extension methods are super important because they enable us type safe exposure
// of various methods on control elements. So you can use the SetText method on only types that actual have the text property
/// <summary>
/// Extension methods that enable fluent UI building with a hierarchical syntax for Flecs entities.
/// </summary>
public static class UIBuilderExtensions
{
    /// <summary>
    /// Creates a new entity with the specified control component and configures it using a builder pattern.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="world">The Flecs world.</param>
    /// <param name="configure">Action to configure the entity and its children.</param>
    /// <returns>The created entity.</returns>
    public static Entity UI<T>(this World world, Action<UIBuilder<T>> configure) where T : AvaloniaObject, new()
    {
        var entity = world.Entity().Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return entity;
    }

    /// <summary>
    /// Creates a new child entity with the specified control component and configures it using a builder pattern.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="configure">Action to configure the entity and its children.</param>
    /// <returns>The created child entity.</returns>
    public static Entity UI<T>(this Entity parent, Action<UIBuilder<T>> configure) where T : Control, new()
    {
        var world = parent.CsWorld();
        var entity = world.Entity().ChildOf(parent).Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return entity;
    }

    /// <summary>
    /// Helper function to set the Column property of a control component;
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetColumn<T>(this UIBuilder<T> builder, int column) where T : Control, new()
    {
        builder.Entity.SetColumn(column);
        return builder;
    }

    /// <summary>
    /// Helper function to set the row property of a control component;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetRow<T>(this UIBuilder<T> builder, int row) where T : Control, new()
    {
        builder.Entity.SetRow(row);
        return builder;
    }

    /// <summary>
    /// Sets the flyout for a button
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="flyout"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetFlyout<T>(this UIBuilder<T> builder, FlyoutBase flyout) where T : Button
    {
        builder.Entity.Get<Button>().Flyout = flyout;
        return builder;
    }

    /// <summary>
    /// Helper function to set the ColumnSpan property
    /// on a Control component that is attach to an entitiy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="columnSpan"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetColumnSpan<T>(this UIBuilder<T> builder, int columnSpan) where T : Control, new()
    {
        builder.Entity.SetColumnSpan(columnSpan);
        return builder;
    }

    /// <summary>
    /// Gets the current column span of an control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="columnSpan"></param>
    /// <returns></returns>
    public static int GetColumnSpan<T>(this UIBuilder<T> builder, int columnSpan) where T : Control, new()
    {
        return builder.Entity.GetColumnSpan();
    }

    /// <summary>
    /// Helper function to set the rowSpan property
    /// on a Control component that is attach to an entitiy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="rowSpan"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetRowSpan<T>(this UIBuilder<T> builder, int rowSpan) where T : Control, new()
    {
        builder.Entity.SetRowSpan(rowSpan);
        return builder;
    }

    /// <summary>
    /// Gets the current row span of an control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="rowSpan"></param>
    /// <returns></returns>
    public static int GetRowSpan<T>(this UIBuilder<T> builder, int rowSpan) where T : Control, new()
    {
        return builder.Entity.GetRowSpan();
    }

    /// <summary>
    /// Sets the text of a TextBlock control.
    /// </summary>
    public static UIBuilder<TextBlock> SetText(this UIBuilder<TextBlock> builder, string text)
    {
        builder.Entity.SetText(text);
        return builder;
    }

    /// <summary>
    /// Attaches an tooltip to an control
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="toolTipEntity"></param>
    /// <returns></returns>
    public static UIBuilder<T> AttachToolTip<T>(this UIBuilder<T> builder, Entity toolTipEntity) where T : Control, new()
    {
        builder.Entity.AttachToolTip(toolTipEntity);
        return builder;
    }

    /// <summary>
    /// Sets the text of a TextBox control.
    /// </summary>
    public static UIBuilder<TextBox> SetText(this UIBuilder<TextBox> builder, string text)
    {
        builder.Entity.SetText(text);
        return builder;
    }

    /// <summary>
    /// Gets the text of a TextBox control.
    /// </summary>
    public static string GetText(this UIBuilder<TextBox> builder)
    {
        return builder.Entity.GetText();
    }

    /// <summary>
    /// Sets the showmode of a flyout
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="flyoutShowMode"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetShowMode<T>(this UIBuilder<T> builder, FlyoutShowMode flyoutShowMode) where T : FlyoutBase
    {
        builder.Entity.SetProperty("ShowMode", flyoutShowMode);
        return builder;
    }

    /// <summary>
    /// Sets the watermark text for a textbox
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="placeholderText"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetWatermark(this UIBuilder<TextBox> builder, string placeholderText)
    {
        builder.Entity.SetWatermark(placeholderText);
        return builder;
    }

    /// <summary>
    /// Helper function to set the orientation of a StackPanel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="orientation"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetOrientation<T>(this UIBuilder<T> builder, Layout.Orientation orientation) where T : StackPanel
    {
        builder.Entity.SetOrientation(orientation);
        return builder;
    }

    /// <summary>
    /// Sets the spacing of the children in a stackpanel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="spacing"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetSpacing<T>(this UIBuilder<T> builder, double spacing) where T : StackPanel
    {
        builder.Entity.SetSpacing(spacing);
        return builder;
    }

    /// <summary>
    /// Sets the TextWrapping for an textblock
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="textWrapping"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetTextWrapping<T>(this UIBuilder<T> builder, TextWrapping textWrapping) where T : TextBlock
    {
        builder.Entity.SetTextWrapping(textWrapping);
        return builder;
    }

    /// <summary>
    /// Enables the control element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<T> Enable<T>(this UIBuilder<T> builder) where T : InputElement
    {
        builder.Entity.EnableInputElement();
        return builder;
    }

    /// <summary>
    /// Disables the control element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<T> Disable<T>(this UIBuilder<T> builder) where T : InputElement
    {
        builder.Entity.DisableInputElement();
        return builder;
    }

    /// <summary>
    /// Sets the fontweight of an textblock
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="fontWeight"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetFontWeight<T>(this UIBuilder<T> builder, FontWeight fontWeight) where T : TextBlock
    {
        builder.Entity.SetFontWeight(fontWeight);
        return builder;
    }

    /// <summary>
    /// Gets the placeholder text for a combobox
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static string GetPlaceholderText(this UIBuilder<ComboBox> builder)
    {
        return builder.Entity.GetPlaceholderText() ?? "";
    }

    /*
    DESIGN HINT:
    When a control element shares a common interface like margin here, we should create a generic. 
    Why? This ensure that automatically any type that uses Control as a base type works without any
    needed special code. When no common interface exists we can simply write the special code like 
    with SetText.
    */

    /// <summary>
    /// Sets the margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, Thickness margin) where T : Control, new()
    {
        builder.Entity.SetMargin(margin);
        return builder;
    }

    /// <summary>
    /// Sets the placeholder text of a ComboBox component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetPlaceholderText<T>(this UIBuilder<T> builder, string text) where T : ComboBox, new()
    {
        builder.Entity.SetPlaceholderText(text);
        return builder;
    }
    /// <summary>
    /// Helper function to set the title of a window
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public static UIBuilder<Window> SetTitle(this UIBuilder<Window> builder, string title)
    {
        builder.Entity.SetWindowTitle(title);
        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<Window> SetPadding(this UIBuilder<Window> builder, double padding)
    {
        builder.Entity.SetPadding(padding);
        return builder;
    }
    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<Window> SetPadding(this UIBuilder<Window> builder, Thickness padding)
    {
        builder.Entity.SetPadding(padding);
        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetPadding<T>(this UIBuilder<T> builder, double padding) where T : Control, new()
    {
        builder.Entity.SetPadding(padding);
        return builder;
    }

    /// <summary>
    /// Set the width
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static UIBuilder<Window> SetWidth(this UIBuilder<Window> builder, double width)
    {
        builder.Entity.SetWidth(width);
        return builder;
    }
    /// <summary>
    /// Set the height
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static UIBuilder<Window> SetHeight(this UIBuilder<Window> builder, double height)
    {
        builder.Entity.SetHeight(height);
        return builder;
    }


    /// <summary>
    /// Sets the margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double margin) where T : Control, new()
    {
        builder.Entity.SetMargin(margin);
        return builder;
    }

    /// <summary>
    /// Sets the horizontal and vertial margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double hMargin, double vMargin) where T : Control, new()
    {
        builder.Entity.SetMargin(
            horizontalMargin: hMargin,
            verticalMargin: vMargin);
        return builder;
    }

    /// <summary>
    /// Sets the left, top, right, bottom margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double lMargin, double tMargin, double rMargin, double bMargin) where T : Control, new()
    {
        builder.Entity.SetMargin(
            leftMargin: lMargin,
            topMargin: tMargin,
            rightMargin: rMargin,
            bottomMargin: bMargin
            );
        return builder;
    }

    /// <summary>
    /// Sets the column definitions of a Grid control.
    /// </summary>
    public static UIBuilder<Grid> SetColumnDefinitions(this UIBuilder<Grid> builder, string columnDefinitions)
    {
        builder.Entity.SetColumnDefinitions(columnDefinitions);
        return builder;
    }

    /// <summary>
    /// Sets the row definitions of a Grid control.
    /// </summary>
    public static UIBuilder<Grid> SetRowDefinitions(this UIBuilder<Grid> builder, string rowDefinitions)
    {
        builder.Entity.SetRowDefinitions(rowDefinitions);
        return builder;
    }


    /// <summary>
    /// Sets the row definitions of a Grid control.
    /// </summary>
    public static UIBuilder<Window> OnWindowClosing(this UIBuilder<Window> builder, Action<object?, WindowClosingEventArgs> handler)
    {
        builder.Entity.OnClosing(handler);
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the OnClick event happens
    /// </summary>
    public static UIBuilder<Button> OnClick(this UIBuilder<Button> builder, Action<object?, Interactivity.RoutedEventArgs> handler)
    {
        builder.Entity.OnClick(handler);
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the OnClick event happens
    /// </summary>
    public static UIBuilder<MenuItem> OnClick(this UIBuilder<MenuItem> builder, Action<object?, Interactivity.RoutedEventArgs> handler)
    {
        builder.Entity.OnClick(handler);
        return builder;
    }

    /// <summary>
    /// When the selection of a combo box changes is the handler executed.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<ComboBox> OnSelectionChanged(this UIBuilder<ComboBox> builder, Action<object?, SelectionChangedEventArgs> handler)
    {
        builder.Entity.OnSelectionChanged(handler);
        return builder;
    }

    /// <summary>
    /// add an callback for the on opened event for a flyout. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnOpened<T>(this UIBuilder<T> builder, Action<object?, EventArgs> handler) where T : FlyoutBase
    {
        builder.Entity.Get<FlyoutBase>().Opened += (sender, e) => handler(sender, e);
        return builder;
    }

    /// <summary>
    /// Hides the flyout
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<T> Hide<T>(this UIBuilder<T> builder) where T : FlyoutBase
    {
        builder.Entity.Get<FlyoutBase>().Hide();
        return builder;
    }

    /// <summary>
    /// Returns the selected item in a listbox
    /// </summary>
    /// <typeparam name="ItemType"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ItemType GetSelectedItem<ItemType>(this UIBuilder<ListBox> builder)
    {
        return builder.Entity.GetSelectedItem<ItemType>();
    }

    /// <summary>
    /// Sets the header
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetHeader<T>(this UIBuilder<T> builder, string header) where T : MenuItem
    {
        builder.Entity.SetHeader(header);
        return builder;
    }


    /// <summary>
    /// Checks if a item list with the selectedItem property has an item selected, if so returns true otherwise false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static bool HasItemSelected<T>(this UIBuilder<T> builder) where T : ListBox
    {
        return builder.Entity.HasItemSelected();
    }



    /// <summary>
    /// Adds an event handler that gets invoked when the OnKeyDown event happens
    /// </summary>
    public static UIBuilder<T> OnKeyDown<T>(this UIBuilder<T> builder, Action<object?, KeyEventArgs> handler) where T : Control, new()
    {
        builder.Entity.OnKeyDown(handler);
        return builder;
    }
    /// <summary>
    /// Occurs asynchronously after text changes and the new text is rendered.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnTextChanged<T>(this UIBuilder<T> builder, Action<object?, TextChangedEventArgs> handler) where T : Control, new()
    {
        builder.Entity.OnTextChanged(handler);
        return builder;
    }

    /// <summary>
    /// Adds an event for IsCheckedChanged for types that implement the ToggleButton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnIsCheckedChanged<T>(this UIBuilder<T> builder, Action<object?, RoutedEventArgs> handler) where T : ToggleButton, new()
    {
        builder.Entity.OnIsCheckedChanged(handler);
        return builder;
    }

    /// <summary>
    /// Sets the data template used to display the items in the ItemsControl.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="template"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetItemTemplate<T>(this UIBuilder<T> builder, IDataTemplate template) where T : ItemsControl
    {
        builder.Entity.SetItemTemplate(template);
        return builder;
    }

    /// <summary>
    /// Sets the inner right content of a TextBox control with 
    /// a control element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerRightContent<T>(this UIBuilder<TextBox> builder, T content) where T : Control, new()
    {
        builder.Entity.SetInnerRightContent(content);
        return builder;
    }

    /// <summary>
    /// Sets the inner right content using an entity.
    /// It uses the object component of an entity as
    /// the content.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content">The object component found in the entity</param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerRightContent(this UIBuilder<TextBox> builder, Entity content)
    {
        builder.Entity.SetInnerRightContent(content.Ensure<object>());
        return builder;
    }

    /// <summary>
    /// Sets the inner right content of a TextBox control
    /// with an object
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerRightContent(this UIBuilder<TextBox> builder, object content)
    {
        builder.Entity.SetInnerRightContent(content);
        return builder;
    }

    /// <summary>
    /// Sets the inner left content using an entity.
    /// It uses the object component of an entity as
    /// the content.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content">The object component found in the entity</param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerLeftContent(this UIBuilder<TextBox> builder, Entity content)
    {
        builder.Entity.SetInnerLeftContent(content.Ensure<object>());
        return builder;
    }

    /// <summary>
    /// Sets the inner left content of a TextBox control
    /// with an object
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerLeftContent(this UIBuilder<TextBox> builder, object content)
    {
        builder.Entity.SetInnerLeftContent(content);
        return builder;
    }

    /// <summary>
    /// Removes the inner right content of a text box
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> RemoveInnerRightContent(this UIBuilder<TextBox> builder)
    {
        builder.Entity.SetInnerRightContent(null);
        return builder;
    }

    /// <summary>
    /// Removes the inner left content of a text box
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> RemoveInnerLeftContent(this UIBuilder<TextBox> builder)
    {
        builder.Entity.SetInnerLeftContent(null);
        return builder;
    }

    /// <summary>
    /// Gets the items of an ItemsControl
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ItemCollection GetItems<T>(this UIBuilder<T> builder) where T : ItemsControl, new()
    {
        return builder.Get<ItemsControl>().Items;
    }

    /// <summary>
    /// Set the min verticalAlignment of the Layoutable component.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="verticalAlignment"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetVerticalAlignment<T>(this UIBuilder<T> builder, VerticalAlignment verticalAlignment) where T : Layoutable, new()
    {
        builder.Entity.SetVerticalAlignment(verticalAlignment);
        return builder;
    }

    /// <summary>
    /// Set the min horizontalAlignment of the Layoutable component.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="horizontalAlignment"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetHorizontalAlignment<T>(this UIBuilder<T> builder, HorizontalAlignment horizontalAlignment) where T : Layoutable, new()
    {
        builder.Entity.SetHorizontalAlignment(horizontalAlignment);
        return builder;
    }

    /// <summary>
    /// Sets the item source for a type that is based on the ItemsControl type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetItemsSource<T>(this UIBuilder<T> builder, System.Collections.IEnumerable collection) where T : ItemsControl
    {
        builder.Entity.SetItemsSource(collection);
        return builder;
    }

    /// <summary>
    /// Returns the itemsource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static System.Collections.IEnumerable GetItemsSource<T>(this UIBuilder<T> builder) where T : ItemsControl
    {
        return builder.Entity.GetItemsSource()!;
    }

    /// <summary>
    /// Sets the selection mode for types that implement SelectingItemsControl
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetSelectionMode<T>(this UIBuilder<T> builder, SelectionMode mode) where T : SelectingItemsControl
    {
        builder.Entity.SetSelectionMode(mode);
        return builder;
    }
    /// <summary>
    /// Sets the context flyout of a control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetContextFlyout<T>(this UIBuilder<T> builder, FlyoutBase content) where T : Control, new()
    {
        builder.Entity.SetContextFlyout(content);
        return builder;
    }

    /// <summary>
    /// Sets the context flyout for a listbox
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<ListBox> SetContextFlyout(this UIBuilder<ListBox> builder, FlyoutBase content)
    {
        builder.Entity.SetContextFlyout(content);
        return builder;
    }
}

/// <summary>
/// A fluent builder for constructing hierarchical UI components as Flecs entities.
/// Provides a clean, nested syntax that visually represents the UI hierarchy.
/// </summary>
/// <typeparam name="T">The type of Avalonia control this builder is configuring.</typeparam>
[Experimental("UIBuilder")]
public class UIBuilder<T> where T : AvaloniaObject
{
    private readonly World _world;
    /// <summary>
    /// Underlying entity
    /// </summary>
    private readonly Entity _entity;
    private readonly T _control;

    /// <summary>
    /// Gets the entity being configured.
    /// </summary>
    public Entity Entity => _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIBuilder{T}"/> class.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    /// <param name="entity">The entity this builder is configuring.</param>
    public UIBuilder(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
        _control = entity.Get<T>();
    }

    /// <summary>
    /// Sets a property on the control by name.
    /// <remarks>
    /// The check if the object implement the property is done at runtime.
    /// Be aware of that. You can use this method, where we didnt already 
    /// implement a type safe way of modifying a property.
    /// </remarks>
    /// </summary>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value to set the property to.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> SetProperty(string name, object value)
    {
        _entity.SetProperty(name, value);
        return this;
    }

    /// <summary>
    /// Gets a property on the control by name.
    /// <remarks>
    /// The check if the object implement the property is done at runtime.
    /// Be aware of that. You can use this method, where we didnt already 
    /// implement a type safe way of getting a property.
    /// </remarks>
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <returns>Returns the property</returns>
    public PropertyType GetProperty<PropertyType>(string propertyName)
    {
        return _entity.GetProperty<PropertyType>(propertyName);
    }

    //TODO: We probably want a way to add childrens without the need
    // to first configure them, this is especially helpful when 
    // we defined our own components.

    /// <summary>
    /// Creates a child control entity and returns its builder.
    /// </summary>
    /// <typeparam name="TChild">The type of child control to create.</typeparam>
    /// <returns>The builder for the newly created child entity.</returns>
    public UIBuilder<TChild> Child<TChild>(Action<UIBuilder<TChild>> configure) where TChild : Control, new()
    {
        var child = _entity.UI(configure);
        return new UIBuilder<TChild>(_world, child);
    }

    /// <summary>
    /// Attaches an ui component as a child.
    /// </summary>
    /// <param name="uIComponent"></param>
    /// <returns></returns>
    public UIBuilder<T> Child(IUIComponent uIComponent)
    {
        uIComponent.Attach(_entity);
        return this;
    }

    /// <summary>
    /// Attaches an entity as a child
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public UIBuilder<T> Child(Entity child)
    {
        child.ChildOf(Entity);
        return this;
    }
    /// <summary>
    /// Attaches an ui component as a child.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public UIBuilder<T> SetComponent<ComponentType>(ComponentType component) where ComponentType : new()
    {
        Entity.Set(component);
        return this;
    }

    /// <summary>
    /// Observe an event on the entity.
    /// </summary>
    /// <typeparam name="EventTypeToObserve"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public UIBuilder<T> Observe<EventTypeToObserve>(Ecs.ObserveEntityCallback callback)
    {
        Entity.Observe<EventTypeToObserve>(callback);
        return this;
    }
    /// <summary>
    /// Emits an event asynchrounsly on the UIThread.
    /// By default it runs events on the UI thread otherwise 
    /// strange behavior may occur.
    /// </summary>
    /// <typeparam name="Event"></typeparam>
    /// <returns></returns>
    public async void EmitAsync<Event>()
    {
        await Dispatcher.UIThread.InvokeAsync(() => Entity.Emit<Event>());
    }

    /// <summary>
    /// Iterate children for ui element.
    /// </summary>
    /// <param name="callback"></param>
    public void Children(Ecs.EachEntityCallback callback)
    {
        Entity.Children(callback);
    }

    /// <summary>
    /// Get managed reference to component value.
    /// </summary>
    /// <typeparam name="ComponentType"></typeparam>
    /// <returns></returns>
    public ref readonly ComponentType Get<ComponentType>()
    {
        return ref Entity.Get<ComponentType>();
    }
}

/* EXAMPLE USAGE

private Entity CreateUILayout()
{
    ContentQueuePage contentQueuePage = new(_world);
    KnowledgeVaultPage knowledgeVaultPage = new(_world);
    SettingsPage settingsPage = new(_world);
    HomePage homePage = new(_world);
    LiteraturePage literaturePage = new(_world);
    SpacedRepetitionPage spacedRepetitionPage = new(_world);

    return _world.UI<NavigationView>(nav => {
        nav.Property("PaneTitle", "Stella Learning")
           .Column(0);
        
        // Child elements are nested in the lambda, showing hierarchy
        nav.Child<ScrollViewer>(scroll => {
            scroll.Child<StackPanel>(stack => {
                stack.Child<Grid>(grid => {
                    grid.Property("ColumnDefinitions", "2,*,*")
                       .Property("RowDefinitions", "Auto");
                });
            });
        });
        
        // Attach pages
        ((IUIComponent)spacedRepetitionPage).Attach(_entity);
        
        // Navigation items are clearly children of nav
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Home");
        });
        
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Knowledge Vault");
        });
        
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Content Queue");
        });
        
        // ... other navigation items and event handlers
    });
}
*/