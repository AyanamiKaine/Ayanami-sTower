using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;

/// <summary>
/// A component added to the IUIComponent's root entity
/// It holds the reference to the disposable component itself.
/// </summary>
public struct DisposableComponentHandle
{
    /// <summary>
    /// When the component's root entity is destroyed, its Dispose method is called automatically.
    /// </summary>
    public IDisposable Target;
}

/// <summary>
/// Helper class to wrap the unsubscribe action
/// </summary>
public class EventSubscriptionToken(Action unsubscribeAction) : IDisposable
{
    private Action? _unsubscribeAction = unsubscribeAction;

    /// <inheritdoc/>
    public void Dispose()
    {
        _unsubscribeAction?.Invoke();
        _unsubscribeAction = null; // Prevent double disposal
    }
}

/// <summary>
/// Component to hold all subscriptions for an entity
/// </summary>
public struct SubscriptionListComponent() : IDisposable
{
    /// <summary>
    /// Subscriptions of the entity
    /// </summary>
    public List<IDisposable> Subscriptions { get; private set; } = [];


    /// <summary>
    /// Method to add a subscription
    /// </summary>
    /// <param name="subscription"></param>
    public void Add(IDisposable subscription)
    {
        Subscriptions ??= []; // Ensure list exists
        Subscriptions.Add(subscription);
    }

    /// <summary>
    /// Dispose method to clean up all subscriptions
    /// </summary>
    public readonly void Dispose()
    {
        if (Subscriptions != null)
        {
            // Dispose in reverse order of addition (optional, but sometimes helpful)
            for (int i = Subscriptions.Count - 1; i >= 0; i--)
            {
                try
                {
                    Subscriptions[i]?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log error during disposal
                    Console.WriteLine($"Error disposing subscription: {ex.Message}");
                }
            }
            Subscriptions.Clear();
        }
    }
}


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

    /*
    Note regarding the AsBaseBuilder method. The problem we face is the following, we want that our
    display right item simply returns a avalonia control class that gets used as a children. But
    our various content displayers, return UIBuilder<MoreSpecificType> like a stack panel. The caller
    shouldnt care for what more specific type gets returned only the base control type matters.

    Calling AsBaseBuilder uses the same underlying entity and fields with the only difference being 
    that the UIBuilder gets converted from the type UIBuilder<StackPanel> => UIBuilder<Control>.
    */

    /// <summary>
    /// Converts a UIBuilder for a derived AvaloniaObject type to a UIBuilder for a base type.
    /// Creates a new UIBuilder instance wrapping the same entity.
    /// </summary>
    /// <typeparam name="TBase">The base AvaloniaObject type for the new builder.</typeparam>
    /// <typeparam name="TDerived">The derived AvaloniaObject type of the source builder.</typeparam>
    /// <param name="derivedBuilder">The source builder instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if derivedBuilder is null.</exception>
    /// <exception cref="ArgumentException">Thrown if derivedBuilder's entity is invalid or dead.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the entity does not have a component of type TBase.</exception>
    public static UIBuilder<TBase> AsBaseBuilder<TBase, TDerived>(this UIBuilder<TDerived> derivedBuilder)
        where TDerived : AvaloniaObject, TBase // Ensure Derived inherits from Base
        where TBase : AvaloniaObject
    {
        if (derivedBuilder == null)
            throw new ArgumentNullException(nameof(derivedBuilder), "Cannot convert a null builder.");

        var entity = derivedBuilder.Entity;

        if (!entity.IsValid() || !entity.IsAlive())
            throw new ArgumentException("Cannot convert a builder with an invalid or dead entity.", nameof(derivedBuilder));

        // Check if the entity has the base component type. Crucial!
        // Use Has<TBase>() because Get<TBase>() might throw if it doesn't exist.
        if (!entity.Has<TBase>())
        {
            // This could happen if the component was removed after the derived builder was created.
            throw new InvalidOperationException($"Entity {entity.Id} ({entity.Name()}) does not have the required base component {typeof(TBase).Name} for conversion.");
        }

        // Get the world instance safely from the entity
        World entityWorld = entity.CsWorld();
        return new UIBuilder<TBase>(entityWorld, entity);
    }

    /// <summary>
    /// Creates a template for a type with a specific root control type.
    /// </summary>
    /// <typeparam name="TData">Type of data to template</typeparam>
    /// <typeparam name="TControl">Type of root control for the template</typeparam>
    /// <param name="world">The Flecs world</param>
    /// <param name="configure">Action to configure the control</param>
    /// <returns>A data template that uses the UIBuilder pattern</returns>
    public static FuncDataTemplate<TData> CreateTemplate<TData, TControl>(
        this World world,
        Action<UIBuilder<TControl>, TData> configure)
        where TControl : Control, new()
    {
        return new FuncDataTemplate<TData>((item, _) =>
        {
            var entity = world.UI<TControl>(builder => configure(builder, item));
            return entity.Get<TControl>();
        });
    }

    /// <summary>
    /// Creates a new entity with the specified control component, configures it using a builder pattern,
    /// and returns the builder for further configuration.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="world">The Flecs world.</param>
    /// <param name="configure">Action to configure the entity and its children using the builder.</param>
    /// <returns>The builder for the created entity.</returns> // CHANGED RETURN TYPE
    public static UIBuilder<T> UI<T>(this World world, Action<UIBuilder<T>> configure) where T : AvaloniaObject, new()
    {
        var entity = world.Entity().Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return builder;
    }

    /// <summary>
    /// Creates a new child entity with the specified control component, configures it using a builder pattern,
    /// and returns the builder for further configuration.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="parentBuilder">The builder for the parent entity.</param> // CHANGED PARAMETER TYPE (for consistency, though Entity also works)
    /// <param name="configure">Action to configure the entity and its children using the builder.</param>
    /// <returns>The builder for the created child entity.</returns> // CHANGED RETURN TYPE
    public static UIBuilder<T> UI<T>(this UIBuilder<AvaloniaObject> parentBuilder, Action<UIBuilder<T>> configure) where T : Control, new() // Generic parent type might be needed
    {
        var world = parentBuilder.Entity.CsWorld(); // Get world from parent builder's entity
        var parentEntity = parentBuilder.Entity;
        var entity = world.Entity().ChildOf(parentEntity).Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return builder;
    }

    /// <summary>
    /// Creates a new child entity with the specified control component, configures it using a builder pattern,
    /// and returns the builder for further configuration.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="configure">Action to configure the entity and its children using the builder.</param>
    /// <returns>The builder for the created child entity.</returns> // CHANGED RETURN TYPE
    public static UIBuilder<T> UI<T>(this Entity parent, Action<UIBuilder<T>> configure) where T : Control, new()
    {
        var world = parent.CsWorld();
        var entity = world.Entity().ChildOf(parent).Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return builder; // RETURN BUILDER
    }

    // --- Helper Method to Add Subscription (using DisposableComponentHandle) ---
    // If you prefer a dedicated list, use the AddSubscription from Approach 1
    private static void AddDisposableSubscription(Entity entity, IDisposable subscription)
    {
        // Option A: Use SubscriptionListComponent (from Approach 1)
        // ref var subListComp = ref entity.Ensure<SubscriptionListComponent>();
        // subListComp.Add(subscription);

        // Option B: Add multiple DisposableComponentHandle? Not ideal.
        // A list component is generally better for multiple disposables.
        // Let's stick with Option A or create a dedicated list component.
        // For simplicity here, let's assume SubscriptionListComponent is used.
        ref var subListComp = ref entity.Ensure<SubscriptionListComponent>();
        subListComp.Add(subscription);

        // --- Alternatively, if DisposableComponentHandle could hold multiple ---
        // --- or if you only expect one disposable per entity (less likely) ---
        // if (entity.Has<DisposableComponentHandle>()) {
        //    // How to handle multiple? Need a list.
        // } else {
        //    entity.Set(new DisposableComponentHandle { Target = subscription });
        // }
        // --> Using a dedicated list component like SubscriptionListComponent is cleaner.
    }

    /// <summary>
    /// Helper function to set the Column property of a control component;
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetColumn<T>(this UIBuilder<T> builder, int column) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        Grid.SetColumn(builder.Get<Control>(), column);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        Grid.SetRow(builder.Get<Control>(), row);
        return builder;
    }

    /// <summary>
    /// Sets the base flyout for a button
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="flyout"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetFlyout<T>(this UIBuilder<T> builder, UIBuilder<FlyoutBase> flyout) where T : Button
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Button>().Flyout = flyout.Get<FlyoutBase>();
        return builder;
    }

    /// <summary>
    /// Sets the menu flyout for a button
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="flyout"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetFlyout<T>(this UIBuilder<T> builder, UIBuilder<MenuFlyout> flyout) where T : Button
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Button>().Flyout = flyout.Get<FlyoutBase>();
        return builder;
    }

    /// <summary>
    /// Sets the flyout for a button
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="entityFlyout">The entity with a flyoutbase component</param>
    /// <returns></returns>
    public static UIBuilder<T> SetFlyout<T>(this UIBuilder<T> builder, Entity entityFlyout) where T : Button
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Button>().Flyout = entityFlyout.Get<FlyoutBase>();
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        Grid.SetColumnSpan(builder.Get<Control>(), columnSpan);
        return builder;
    }

    /// <summary>
    /// Shows a window
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<Window> Show(this UIBuilder<Window> builder)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Window>().Show();
        return builder;
    }

    /// <summary>
    /// Gets the current column span of an control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static int GetColumnSpan<T>(this UIBuilder<T> builder) where T : Control, new()
    {
        return Grid.GetColumnSpan(builder.Get<Control>());
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
        Grid.SetRowSpan(builder.Get<Control>(), rowSpan);
        return builder;
    }

    /// <summary>
    /// Gets the current row span of an control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static int GetRowSpan<T>(this UIBuilder<T> builder) where T : Control, new()
    {
        return Grid.GetRowSpan(builder.Get<Control>());
    }

    /// <summary>
    /// Sets the text of a TextBlock control.
    /// </summary>
    public static UIBuilder<TextBlock> SetText(this UIBuilder<TextBlock> builder, string text)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBlock>().Text = text;
        return builder;
    }

    /// <summary>
    /// Sets the text of a button control by adding a textblock control to it.
    /// This is done so you can easily say button.SetFontSize() and changing the 
    /// text size of the textblock
    /// </summary>
    public static UIBuilder<Button> SetText(this UIBuilder<Button> builder, string text)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Child<TextBlock>((t) =>
        {
            t.SetText(text);
        });
        return builder;
    }

    /// <summary>
    /// Sets the IsChecked property of a ToggleButton to false.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<ToggleButton> UnCheck(this UIBuilder<ToggleButton> builder)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<ToggleButton>().IsChecked = false;
        return builder;
    }

    /// <summary>
    /// Sets the IsChecked property of a ToggleButton to true.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<ToggleButton> Check(this UIBuilder<ToggleButton> builder)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<ToggleButton>().IsChecked = true;
        return builder;
    }

    /// <summary>
    /// Sets the foreground brush of a TemplatedControl.
    /// </summary>
    /// <typeparam name="T">The type of TemplatedControl</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="brush">The brush to set as the foreground</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetForeground<T>(this UIBuilder<T> builder, IBrush brush) where T : TemplatedControl
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().Foreground = brush;
        return builder;
    }
    /// <summary>
    /// Sets the foreground brush of a TextBlock.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="brush">The brush to set as the foreground</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TextBlock> SetForeground(this UIBuilder<TextBlock> builder, IBrush brush)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBlock>().Foreground = brush;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        if (toolTipEntity.Has<ToolTip>())
        {
            ToolTip.SetTip(builder.Get<Control>(), toolTipEntity.Get<ToolTip>());
            return builder;
        }
        return builder;
    }

    /// <summary>
    /// Attaches an tooltip to an control
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="toolTip"></param>
    /// <returns></returns>
    public static UIBuilder<T> AttachToolTip<T>(this UIBuilder<T> builder, UIBuilder<ToolTip> toolTip) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        if (toolTip.Entity.Has<ToolTip>())
        {
            ToolTip.SetTip(builder.Get<Control>(), toolTip.Get<ToolTip>());
            return builder;
        }
        return builder;
    }

    /// <summary>
    /// Sets the visibility of a Visual control.
    /// </summary>
    /// <typeparam name="T">The type of Visual control</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="isVisible">Whether the control should be visible (default: true)</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> Visible<T>(this UIBuilder<T> builder, bool isVisible = true) where T : Visual, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().IsVisible = isVisible;
        return builder;
    }

    /// <summary>
    /// Sets the text of a TextBox control.
    /// </summary>
    public static UIBuilder<TextBox> SetText(this UIBuilder<TextBox> builder, string text)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBox>().Text = text;
        return builder;
    }

    /// <summary>
    /// Gets the text of a TextBox control.
    /// </summary>
    public static string GetText(this UIBuilder<TextBox> builder)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return "";

        return builder.Get<TextBox>().Text ?? "";
    }

    /// <summary>
    /// Sets the showmode of a flyout
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="flyoutShowMode"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetShowMode<T>(this UIBuilder<T> builder, FlyoutShowMode flyoutShowMode) where T : MenuFlyout
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<MenuFlyout>().ShowMode = flyoutShowMode;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBox>().Watermark = placeholderText;
        return builder;
    }

    /// <summary>
    /// Sets the watermark text for a auto complete box
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="placeholderText"></param>
    /// <returns></returns>
    public static UIBuilder<AutoCompleteBox> SetWatermark(this UIBuilder<AutoCompleteBox> builder, string placeholderText)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<AutoCompleteBox>().Watermark = placeholderText;
        return builder;
    }

    /// <summary>
    /// Set the wrapping of the text
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="textWrapping"></param>
    /// <returns></returns>
    public static UIBuilder<TextBlock> SetTextWrapping(this UIBuilder<TextBlock> builder, TextWrapping textWrapping)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBlock>().TextWrapping = textWrapping;
        return builder;
    }
    /// <summary>
    /// Set the trimming of the text
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="textTrimming"></param>
    /// <returns></returns>
    public static UIBuilder<TextBlock> SetTextTrimming(this UIBuilder<TextBlock> builder, TextTrimming textTrimming)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBlock>().TextTrimming = textTrimming;
        return builder;
    }

    /// <summary>
    /// Sets the text alignment for a TextBlock.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="textAlignment">The text alignment to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TextBlock> SetTextAlignment(this UIBuilder<TextBlock> builder, TextAlignment textAlignment)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBlock>().TextAlignment = textAlignment;
        return builder;
    }


    /// <summary>
    /// Set the wrapping of the text of a button
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="textWrapping"></param>
    /// <returns></returns>
    public static UIBuilder<Button> SetTextWrapping(this UIBuilder<Button> builder, TextWrapping textWrapping)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Child<TextBlock>((t) =>
        {
            t.SetTextWrapping(textWrapping);
        });
        return builder;
    }

    /// <summary>
    /// Set the wrapping of the text
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="textWrapping"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetTextWrapping(this UIBuilder<TextBox> builder, TextWrapping textWrapping)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<TextBox>().TextWrapping = textWrapping;
        return builder;
    }

    /// <summary>
    /// Set the fill of a shape
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="fill"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetFill<T>(this UIBuilder<T> builder, IBrush fill) where T : Shape
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Shape>().Fill = fill;
        return builder;
    }

    /// <summary>
    /// Set the Stroke of a shape
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="stroke"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetStroke<T>(this UIBuilder<T> builder, IBrush stroke) where T : Shape
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Shape>().Stroke = stroke;
        return builder;
    }

    /// <summary>
    /// Set the stroke thickness of a shape
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="strokeThickness"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetStrokeThickness<T>(this UIBuilder<T> builder, double strokeThickness) where T : Shape
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Shape>().StrokeThickness = strokeThickness;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Orientation = orientation;
        return builder;
    }

    /// <summary>
    /// Sets the height of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="height">The height value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetHeight<T>(this UIBuilder<T> builder, double height) where T : Layoutable
    {
        builder.Get<T>().Height = height;
        return builder;
    }

    /// <summary>
    /// Sets the minimum heigth of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="minHeigth">The minimum height value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetMinHeigth<T>(this UIBuilder<T> builder, double minHeigth) where T : Layoutable
    {
        builder.Get<T>().MinHeight = minHeigth;
        return builder;
    }

    /// <summary>
    /// Sets the maximum height of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="maxHeigth">The maximum height value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetMaxHeigth<T>(this UIBuilder<T> builder, double maxHeigth) where T : Layoutable
    {
        builder.Get<T>().MaxHeight = maxHeigth;
        return builder;
    }

    /// <summary>
    /// Sets the minimum width of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="minWidth">The minimum width value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetMinWidth<T>(this UIBuilder<T> builder, double minWidth) where T : Layoutable
    {
        builder.Get<T>().MinWidth = minWidth;
        return builder;
    }

    /// <summary>
    /// Sets the maximum width of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="maxWidth">The maximum width value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetMaxWidth<T>(this UIBuilder<T> builder, double maxWidth) where T : Layoutable
    {
        builder.Get<T>().MaxWidth = maxWidth;
        return builder;
    }

    /// <summary>
    /// Sets the width of a Layoutable control.
    /// </summary>
    /// <typeparam name="T">The type of Layoutable</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="width">The width value to set</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetWidth<T>(this UIBuilder<T> builder, double width) where T : Layoutable
    {
        builder.Get<T>().Width = width;
        return builder;
    }

    /// <summary>
    /// Sets the background brush of a Panel control.
    /// </summary>
    /// <typeparam name="T">The type of Panel</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="background">The brush to set as the background</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetBackground<T>(this UIBuilder<T> builder, IBrush background) where T : Panel
    {
        builder.Get<T>().Background = background;
        return builder;
    }

    /// <summary>
    /// Sets the background brush of a button control.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="background">The brush to set as the background</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<Button> SetBackground(this UIBuilder<Button> builder, IBrush background)
    {
        builder.Get<Button>().Background = background;
        return builder;
    }

    /// <summary>
    /// Sets the background brush of a Panel control.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="background">The brush to set as the background</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<Border> SetBackground(this UIBuilder<Border> builder, IBrush background)
    {
        builder.Get<Border>().Background = background;
        return builder;
    }

    /// <summary>
    /// Removes the background brush of a Panel control by setting it to null.
    /// </summary>
    /// <typeparam name="T">The type of Panel</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> RemoveBackground<T>(this UIBuilder<T> builder) where T : Panel
    {
        builder.Get<T>().Background = null;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Spacing = spacing;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.DisableInputElement();
        return builder;
    }

    /// <summary>
    /// Sets the image that will be displayed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetSource<T>(this UIBuilder<T> builder, IImage? source) where T : Image
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Image>().Source = source;
        return builder;
    }


    /// <summary>
    /// Sets the IsHitTestVisible property of an InputElement.
    /// </summary>
    /// <typeparam name="T">The type of InputElement</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="isHitTestVisible">Whether the element should be hit test visible</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetIsHitTestVisible<T>(this UIBuilder<T> builder, bool isHitTestVisible) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().IsHitTestVisible = isHitTestVisible;
        return builder;
    }

    /// <summary>
    /// Removes the image that will be displayed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveSource<T>(this UIBuilder<T> builder) where T : Image
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Image>().Source = null;
        return builder;
    }

    /// <summary>
    /// Gets the image that will be displayed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IImage? GetSource<T>(this UIBuilder<T> builder) where T : Image
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return null;

        return builder.Get<Image>().Source ?? null;
    }

    /// <summary>
    /// Adds an on pointer pressed event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerPressed"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerPressed<T>(this UIBuilder<T> builder, EventHandler<Input.PointerPressedEventArgs>? onPointerPressed) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerPressed += onPointerPressed;
        return builder;
    }
    /// <summary>
    /// Removes an on pointer pressed event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerPressed"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveOnPointerPressed<T>(this UIBuilder<T> builder, EventHandler<Input.PointerPressedEventArgs>? onPointerPressed) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerPressed -= onPointerPressed;
        return builder;
    }

    /// <summary>
    /// Adds an on pointer moved event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerMoved"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerMoved<T>(this UIBuilder<T> builder, EventHandler<Input.PointerEventArgs>? onPointerMoved) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerMoved += onPointerMoved;
        return builder;
    }
    /// <summary>
    /// Adds an on pointer moved event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerMoved"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveOnPointerMoved<T>(this UIBuilder<T> builder, EventHandler<Input.PointerEventArgs>? onPointerMoved) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerMoved += onPointerMoved;
        return builder;
    }

    /// <summary>
    /// Adds an on pointer released event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerRelease"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerReleased<T>(this UIBuilder<T> builder, EventHandler<Input.PointerReleasedEventArgs>? onPointerRelease) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerReleased += onPointerRelease;
        return builder;
    }

    /// <summary>
    /// Remove an on pointer released event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerRelease"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveOnPointerReleased<T>(this UIBuilder<T> builder, EventHandler<Input.PointerReleasedEventArgs>? onPointerRelease) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerReleased -= onPointerRelease;
        return builder;
    }

    /// <summary>
    /// Adds an on pointer entered event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerEntered"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerEntered<T>(this UIBuilder<T> builder, EventHandler<Input.PointerEventArgs>? onPointerEntered) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerEntered += onPointerEntered;
        return builder;
    }

    //TODO: IMPLEMENT WEAK EVENT HANDLERS for diposables

    /// <summary>
    /// Adds an on pointer exited event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPoninterExited"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerExited<T>(this UIBuilder<T> builder, EventHandler<Input.PointerEventArgs>? onPoninterExited) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerExited += onPoninterExited;
        return builder;
    }

    /// <summary>
    /// Remove an on pointer entered event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerEntered"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveOnPointerEntered<T>(this UIBuilder<T> builder, EventHandler<Input.PointerEventArgs>? onPointerEntered) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerEntered -= onPointerEntered;
        return builder;
    }

    /// <summary>
    /// Adds an on pointer capture lost event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerCaptureLost"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnPointerCaptureLost<T>(this UIBuilder<T> builder, EventHandler<Input.PointerCaptureLostEventArgs>? onPointerCaptureLost) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerCaptureLost += onPointerCaptureLost;
        return builder;
    }

    /// <summary>
    /// Remove an on pointer capture lost event to the input element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="onPointerCaptureLost"></param>
    /// <returns></returns>
    public static UIBuilder<T> RemoveOnPointerCaptureLost<T>(this UIBuilder<T> builder, EventHandler<Input.PointerCaptureLostEventArgs>? onPointerCaptureLost) where T : InputElement
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<InputElement>().PointerCaptureLost -= onPointerCaptureLost;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().FontWeight = fontWeight;
        return builder;
    }

    /// <summary>
    /// Sets the font size of a TextBlock.
    /// </summary>
    /// <typeparam name="T">The type of TextBlock</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="fontSize">The size to set for the font</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetFontSize<T>(this UIBuilder<T> builder, double fontSize) where T : TextBlock
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().FontSize = fontSize;
        return builder;
    }

    /// <summary>
    /// Sets the font opacity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="opacity"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetOpacity<T>(this UIBuilder<T> builder, double opacity) where T : TextBlock
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().Opacity = opacity;
        return builder;
    }

    /// <summary>
    /// Sets the fontsize for the buttons textblock
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="fontSize"></param>
    /// <returns></returns>
    public static UIBuilder<Button> SetFontSize(this UIBuilder<Button> builder, double fontSize)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Children((child) =>
        {
            if (child.Has<TextBlock>())
                child.Get<TextBlock>().FontSize = fontSize;
        });

        return builder;
    }

    /// <summary>
    /// Sets the fontsize for the buttons textblock
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cornerRadius"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetCornerRadius(this UIBuilder<Border> builder, double cornerRadius)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().CornerRadius = new CornerRadius(cornerRadius);

        return builder;
    }

    /// <summary>
    /// Sets the fontsize for the buttons textblock
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cornerRadius"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetCornerRadius(this UIBuilder<Border> builder, CornerRadius cornerRadius)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().CornerRadius = cornerRadius;

        return builder;
    }

    /// <summary>
    /// Sets the font size of a TextBox.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="fontSize">The size to set for the font</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TextBox> SetFontSize(this UIBuilder<TextBox> builder, double fontSize)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().FontSize = fontSize;
        return builder;
    }

    /// <summary>
    /// Sets a value that determines whether the TextBox allows and displays newline or return characters
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="acceptsReturn"></param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TextBox> AcceptsReturn(this UIBuilder<TextBox> builder, bool acceptsReturn = true)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().AcceptsReturn = acceptsReturn;
        return builder;
    }

    /// <summary>
    /// Gets the placeholder text for a combobox
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static string GetPlaceholderText(this UIBuilder<ComboBox> builder)
    {
        return builder.Get<ComboBox>().PlaceholderText ?? "";
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Margin = margin;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().PlaceholderText = text;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Window>().Title = title;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Window>().Padding = new Thickness(padding);

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Window>().Padding = padding;

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetPadding(this UIBuilder<Border> builder, Thickness padding)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().Padding = padding;

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="horizontalPadding"></param>
    /// <param name="verticalPadding"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetPadding(this UIBuilder<Border> builder, double horizontalPadding, double verticalPadding)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().Padding = new Thickness(horizontalPadding, verticalPadding);

        return builder;
    }


    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetPadding(this UIBuilder<Border> builder, double left, double top, double right, double bottom)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().Padding = new Thickness(left, top, right, bottom);

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<Border> SetPadding(this UIBuilder<Border> builder, double padding)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Border>().Padding = new Thickness(padding);

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetPadding<T>(this UIBuilder<T> builder, double padding) where T : TemplatedControl, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Padding = new Thickness(padding);
        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="horizontalPadding"></param>
    /// <param name="verticalPadding"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetPadding<T>(this UIBuilder<T> builder, double horizontalPadding, double verticalPadding) where T : TemplatedControl, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Padding = new Thickness(horizontalPadding, verticalPadding);

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="horizontalPadding"></param>
    /// <param name="verticalPadding"></param>
    /// <returns></returns>
    public static UIBuilder<Decorator> SetPadding(this UIBuilder<Decorator> builder, double horizontalPadding, double verticalPadding)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Decorator>().Padding = new Thickness(horizontalPadding, verticalPadding);

        return builder;
    }

    /// <summary>
    /// Set padding
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="leftPadding"></param>
    /// <param name="topPadding"></param>
    /// <param name="rightPadding"></param>
    /// <param name="bottomPadding"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetPadding<T>(this UIBuilder<T> builder, double leftPadding, double topPadding, double rightPadding, double bottomPadding) where T : TemplatedControl, new()
    {
        builder.Get<T>().Padding = new Thickness(leftPadding, topPadding, rightPadding, bottomPadding);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Window>().Width = width;
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
        builder.Get<Window>().Height = height;
        return builder;
    }


    /// <summary>
    /// Sets the margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double margin) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Margin = new Thickness(margin);
        return builder;
    }

    /// <summary>
    /// Sets the horizontal and vertial margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double hMargin, double vMargin) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Margin = new Thickness(hMargin, vMargin);

        return builder;
    }

    /// <summary>
    /// Sets the left, top, right, bottom margin of a control.
    /// </summary>
    public static UIBuilder<T> SetMargin<T>(this UIBuilder<T> builder, double lMargin, double tMargin, double rMargin, double bMargin) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Margin = new Thickness(lMargin, tMargin, rMargin, bMargin);

        return builder;
    }

    /// <summary>
    /// Sets the column definitions of a Grid control.
    /// </summary>
    public static UIBuilder<Grid> SetColumnDefinitions(this UIBuilder<Grid> builder, string columnDefinitions)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Grid>().ColumnDefinitions = new ColumnDefinitions(columnDefinitions);
        return builder;
    }

    /// <summary>
    /// Sets the row definitions of a Grid control.
    /// </summary>
    public static UIBuilder<Grid> SetRowDefinitions(this UIBuilder<Grid> builder, string rowDefinitions)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<Grid>().RowDefinitions = new RowDefinitions(rowDefinitions);
        return builder;
    }


    /// <summary>
    /// Adds an event handler that gets invoked when the Closing event happens. For Windows this happens
    /// BEFORE the window is fully closed but AFTER the window tries to close. When the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    public static UIBuilder<Window> OnClosing(this UIBuilder<Window> builder, EventHandler<WindowClosingEventArgs> handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var window = builder.Get<Window>();

        // Create an observable from the Closing event
        var closingObservable = Observable.FromEventPattern<EventHandler<WindowClosingEventArgs>, WindowClosingEventArgs>(
            h => window.Closing += h,
            h => window.Closing -= h);

        // Subscribe the lambda and get the disposable token
        var subscription = closingObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs)); // Pass EventArgs directly

        // Add the subscription token to the entity for automatic disposal
        AddDisposableSubscription(builder.Entity, subscription);
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the Openend event happens. For Windows this happens
    /// After the window is fully opened. When the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    public static UIBuilder<Window> OnOpened(this UIBuilder<Window> builder, EventHandler handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var window = builder.Get<Window>();

        // Create an observable from the Opened event
        var openedObservable = Observable.FromEventPattern<RoutedEventArgs>(
            addHandler => window.Opened += handler,
            removeHandler => window.Opened -= handler);

        // Subscribe the lambda and get the disposable token
        var subscription = openedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs)); // Pass EventArgs directly

        // Add the subscription token to the entity for automatic disposal
        AddDisposableSubscription(builder.Entity, subscription);
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the Closed event happens. For Windows this happens
    /// AFTER the window is fully closed. When the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    public static UIBuilder<Window> OnClosed(this UIBuilder<Window> builder, EventHandler handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var window = builder.Get<Window>();

        // Create an observable from the Closed event
        var closedObservable = Observable.FromEventPattern<RoutedEventArgs>(
            addHandler => window.Closed += handler,
            removeHandler => window.Closed -= handler);

        // Subscribe the lambda and get the disposable token
        var subscription = closedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs)); // Pass EventArgs directly

        // Add the subscription token to the entity for automatic disposal
        AddDisposableSubscription(builder.Entity, subscription);
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the Click event happens, when the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    public static UIBuilder<Button> OnClick(this UIBuilder<Button> builder, Action<object?, Interactivity.RoutedEventArgs> handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var control = builder.Get<Button>();

        // Create an observable from the Click event
        var clickObservable = Observable.FromEventPattern<RoutedEventArgs>(
            addHandler => control.Click += addHandler,
            removeHandler => control.Click -= removeHandler);

        // Subscribe the lambda and get the disposable token
        var subscription = clickObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs)); // Pass EventArgs directly

        // Add the subscription token to the entity for automatic disposal
        AddDisposableSubscription(builder.Entity, subscription);

        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the OnClick event happens, when the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    public static UIBuilder<MenuItem> OnClick(this UIBuilder<MenuItem> builder, Action<object?, Interactivity.RoutedEventArgs> handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var control = builder.Get<MenuItem>();

        // Create an observable from the Click event
        var clickObservable = Observable.FromEventPattern<RoutedEventArgs>(
            addHandler => control.Click += addHandler,
            removeHandler => control.Click -= removeHandler);

        // Subscribe the lambda and get the disposable token
        var subscription = clickObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs)); // Pass EventArgs directly

        // Add the subscription token to the entity for automatic disposal
        AddDisposableSubscription(builder.Entity, subscription);

        return builder;
    }

    /// <summary>
    /// When the selection of a combo box changes is the handler executed., when the
    /// underlying entity gets destroyed it automatically disposes the event handler.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<ComboBox> OnSelectionChanged(this UIBuilder<ComboBox> builder, Action<object?, SelectionChangedEventArgs> handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var control = builder.Get<ComboBox>();

        var checkedChangedObservable = Observable.FromEventPattern<SelectionChangedEventArgs>(
            addHandler => control.SelectionChanged += addHandler,
            removeHandler => control.SelectionChanged -= removeHandler);

        var subscription = checkedChangedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs));

        AddDisposableSubscription(builder.Entity, subscription);
        return builder;
    }

    /// <summary>
    /// When the selection of a ListBox changes, the handler is executed.
    /// When the underlying entity gets destroyed, it automatically disposes the event handler.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="handler">The event handler to invoke when selection changes</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<ListBox> OnSelectionChanged(this UIBuilder<ListBox> builder, Action<object?, SelectionChangedEventArgs> handler)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var control = builder.Get<ListBox>();

        var checkedChangedObservable = Observable.FromEventPattern<SelectionChangedEventArgs>(
            addHandler => control.SelectionChanged += addHandler,
            removeHandler => control.SelectionChanged -= removeHandler);

        var subscription = checkedChangedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs));

        AddDisposableSubscription(builder.Entity, subscription);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<FlyoutBase>().Hide();
        return builder;
    }

    /// <summary>
    /// Returns the selected item in a listbox
    /// </summary>
    /// <typeparam name="ItemType"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ItemType GetSelectedItem<ItemType>(this UIBuilder<ListBox> builder) where ItemType : new()
    {
        return builder.Get<ListBox>().SelectedItem is ItemType item ? item : new ItemType();
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<T>().Header = header;

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
        return builder.Get<ListBox>().SelectedItem is not null;
    }

    /// <summary>
    /// Sets the border thickness of a TemplatedControl.
    /// </summary>
    /// <typeparam name="T">The type of TemplatedControl</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="borderThickness">The thickness to set for the border</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetBorderThickness<T>(this UIBuilder<T> builder, Thickness borderThickness) where T : TemplatedControl
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().BorderThickness = borderThickness;
        return builder;
    }

    /// <summary>
    /// Sets the border thickness of a border.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="borderThickness">The thickness to set for the border</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<Border> SetBorderThickness(this UIBuilder<Border> builder, Thickness borderThickness)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Border>().BorderThickness = borderThickness;
        return builder;
    }

    /// <summary>
    /// Sets the border brush of a TemplatedControl.
    /// </summary>
    /// <typeparam name="T">The type of TemplatedControl</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="brush">The brush to set as the border</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetBorderBrush<T>(this UIBuilder<T> builder, IBrush brush) where T : TemplatedControl
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().BorderBrush = brush;
        return builder;
    }

    /// <summary>
    /// Sets the border brush of a Border.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="brush">The brush to set as the border</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<Border> SetBorderBrush(this UIBuilder<Border> builder, IBrush brush)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Border>().BorderBrush = brush;
        return builder;
    }

    /// <summary>
    /// Adds an event handler that gets invoked when the OnKeyDown event happens
    /// </summary>
    public static UIBuilder<T> OnKeyDown<T>(this UIBuilder<T> builder, Action<object?, KeyEventArgs> handler) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var control = builder.Get<T>();

        var keyDownObservable = Observable.FromEventPattern<KeyEventArgs>(
            addHandler => control.KeyDown += addHandler,
            removeHandler => control.KeyDown -= removeHandler);

        var subscription = keyDownObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs));

        AddDisposableSubscription(builder.Entity, subscription);
        return builder;
    }
    /// <summary>
    /// Occurs asynchronously after text changes and the new text is rendered.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<T> OnTextChanged<T>(this UIBuilder<T> builder, Action<object?, TextChangedEventArgs> handler) where T : TextBox, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var textBox = builder.Get<T>();

        var textChangedObservable = Observable.FromEventPattern<TextChangedEventArgs>(
            addHandler => textBox.TextChanged += addHandler,
            removeHandler => textBox.TextChanged -= removeHandler);

        var subscription = textChangedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs));

        AddDisposableSubscription(builder.Entity, subscription);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        var toggleButton = builder.Get<T>();

        var checkedChangedObservable = Observable.FromEventPattern<RoutedEventArgs>(
            addHandler => toggleButton.IsCheckedChanged += addHandler,
            removeHandler => toggleButton.IsCheckedChanged -= removeHandler);

        var subscription = checkedChangedObservable
            .ObserveOn(AvaloniaScheduler.Instance) // Ensure execution on UI thread
            .Subscribe(eventPattern => handler(eventPattern.Sender, eventPattern.EventArgs));

        AddDisposableSubscription(builder.Entity, subscription);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().ItemTemplate = template;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().InnerRightContent = content;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().InnerRightContent = content.Ensure<object>();

        return builder;
    }

    /// <summary>
    /// Sets the inner right content of a TextBox control
    /// with an object
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerRightContent<T>(this UIBuilder<TextBox> builder, UIBuilder<T> content) where T : AvaloniaObject
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().InnerRightContent = content.Get<T>();

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;
        builder.Entity.Get<TextBox>().InnerLeftContent = content.Ensure<object>();

        return builder;
    }

    /// <summary>
    /// Sets the inner left content of a TextBox control
    /// with an object
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> SetInnerLeftContent<T>(this UIBuilder<TextBox> builder, UIBuilder<T> content) where T : AvaloniaObject
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<TextBox>().InnerLeftContent = content.Get<T>();

        return builder;
    }

    /// <summary>
    /// Removes the inner right content of a text box
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> RemoveInnerRightContent(this UIBuilder<TextBox> builder)
    {
        builder.Entity.Get<TextBox>().InnerRightContent = null;
        return builder;
    }

    /// <summary>
    /// Removes the inner left content of a text box
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static UIBuilder<TextBox> RemoveInnerLeftContent(this UIBuilder<TextBox> builder)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;
        builder.Entity.Get<TextBox>().InnerLeftContent = null;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().VerticalAlignment = verticalAlignment;
        return builder;
    }

    /// <summary>
    /// Sets the horizontal scroll bar visibility for a ScrollViewer.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="scrollBarVisibility">The visibility mode for the horizontal scrollbar</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<ScrollViewer> SetHorizontalScrollBarVisibility(this UIBuilder<ScrollViewer> builder, ScrollBarVisibility scrollBarVisibility)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<ScrollViewer>().HorizontalScrollBarVisibility = scrollBarVisibility;
        return builder;
    }


    /// <summary>
    /// Sets the stretch mode of an Image control.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="stretch">The stretch mode to apply to the image</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<Image> SetStretch(this UIBuilder<Image> builder, Stretch stretch)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<Image>().Stretch = stretch;
        return builder;
    }

    /// <summary>
    /// Sets the vertical scroll bar visibility for a ScrollViewer.
    /// </summary>
    /// <param name="builder">The UI builder</param>
    /// <param name="scrollBarVisibility">The visibility mode for the vertical scrollbar</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<ScrollViewer> SetVerticalScrollBarVisibility(this UIBuilder<ScrollViewer> builder, ScrollBarVisibility scrollBarVisibility)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<ScrollViewer>().VerticalScrollBarVisibility = scrollBarVisibility;
        return builder;
    }

    /// <summary>
    /// Sets the dock property of a control when it's placed in a DockPanel.
    /// </summary>
    /// <typeparam name="T">The type of Control</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="dock">The dock position (Top, Left, Right, Bottom)</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<T> SetDock<T>(this UIBuilder<T> builder, Dock dock) where T : Control, new()
    {
        DockPanel.SetDock(builder.Entity.Get<T>(), dock);
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().HorizontalAlignment = horizontalAlignment;
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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().ItemsSource = collection;
        return builder;
    }

    /// <summary>
    /// Sets a binding between a property on the control and a property path on the control's DataContext.
    /// </summary>
    /// <typeparam name="TControl">The type of control to set the binding for</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="targetProperty">The Avalonia property to bind to (e.g., TextBlock.TextProperty)</param>
    /// <param name="sourcePropertyPath">The property path on the DataContext (e.g., "Name")</param>
    /// <param name="mode">The binding mode (default: OneWay)</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TControl> SetBinding<TControl>(
        this UIBuilder<TControl> builder,
        AvaloniaProperty targetProperty, // e.g., TextBlock.TextProperty
        string sourcePropertyPath,      // e.g., "Name"
        BindingMode mode = BindingMode.OneWay) where TControl : AvaloniaObject
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive()) return builder;

        var control = builder.Entity.Get<TControl>(); // Get the Avalonia control
        var binding = new Binding(sourcePropertyPath) { Mode = mode };
        control.Bind(targetProperty, binding); // Use Avalonia's Bind method

        return builder;
    }

    /// <summary>
    /// Sets a binding between a property on the control and a binding object.
    /// </summary>
    /// <typeparam name="TControl">The type of control to set the binding for</typeparam>
    /// <param name="builder">The UI builder</param>
    /// <param name="targetProperty">The Avalonia property to bind to (e.g., TextBlock.TextProperty)</param>
    /// <param name="binding">The binding object to use</param>
    /// <returns>The builder for method chaining</returns>
    public static UIBuilder<TControl> SetBinding<TControl>(
        this UIBuilder<TControl> builder,
        AvaloniaProperty targetProperty, // e.g., TextBlock.TextProperty
        Binding binding) where TControl : AvaloniaObject
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive()) return builder;

        var control = builder.Entity.Get<TControl>(); // Get the Avalonia control
        control.Bind(targetProperty, binding); // Use Avalonia's Bind method

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
        return builder.Entity.Get<T>().ItemsSource!;
    }

    /// <summary>
    /// Sets the selection mode for types that implement SelectingItemsControl
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static UIBuilder<T> SetSelectionMode<T>(this UIBuilder<T> builder, SelectionMode mode) where T : ListBox
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;
        builder.Entity.Get<T>().SelectionMode = mode;

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
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().ContextFlyout = content;
        return builder;
    }
    /// <summary>
    /// Sets the context flyout of a control component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="contextFlyoutEntity"></param>
    public static UIBuilder<T> SetContextFlyout<T>(this UIBuilder<T> builder, Entity contextFlyoutEntity) where T : Control, new()
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().ContextFlyout = contextFlyoutEntity.Get<FlyoutBase>();
        return builder;
    }

    /// <summary>
    /// Sets the context flyout of a control component
    /// </summary>
    /// <typeparam name="T">The type of control to set the context flyout for</typeparam>
    /// <typeparam name="flyoutType">The type of flyout to set as the context</typeparam>
    /// <param name="builder">The UI builder for the control</param>
    /// <param name="flyoutBuilder">The UI builder for the flyout</param>
    /// <returns>The UI builder for the control, for method chaining</returns>
    public static UIBuilder<T> SetContextFlyout<T, flyoutType>(this UIBuilder<T> builder, UIBuilder<flyoutType> flyoutBuilder) where T : Control, new() where flyoutType : FlyoutBase
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Entity.Get<T>().ContextFlyout = flyoutBuilder.Get<FlyoutBase>();
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
        builder.Get<ListBox>().ContextFlyout = content;
        return builder;
    }

    // Helper method to create and add the style
    private static UIBuilder<T> AddStyleInternal<T>(
        this UIBuilder<T> builder,
        Func<Selector?, Selector> selectorBuilder, // Function to build the selector
        IEnumerable<(AvaloniaProperty Property, object Value)> setters) where T : Control // Use Control constraint for Styles
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive())
            return builder;

        var control = builder.Get<Control>(); // Get the base Control

        var style = new Style(selectorBuilder); // Use the provided selector builder

        // Add setters to the collection rather than assigning to the property
        foreach (var (Property, Value) in setters)
        {
            style.Setters.Add(new Setter(Property, Value));
        }

        control.Styles.Add(style); // Add the style to the control's specific Styles collection
        return builder;
    }

    /// <summary>
    /// Applies a style directly to the control type (no pseudo-class).
    /// NOTE: For state-dependent properties (Background, Foreground, BorderBrush on :pointerover, etc.),
    /// default theme styles might use more specific selectors targeting template parts, potentially overriding this style.
    /// Consider using more specific styling methods if needed.
    /// Example: buttonBuilder.SetBaseStyle((Button.FontSizeProperty, 14.0), (Button.FontWeightProperty, FontWeight.Bold));
    /// </summary>
    public static UIBuilder<T> SetBaseStyle<T>(
        this UIBuilder<T> builder,
        params (AvaloniaProperty Property, object Value)[] setters) where T : Control
    {
        // Selector for the base type: s => s.OfType<T>()
        return builder.AddStyleInternal(s => s.OfType<T>(), setters);
    }

    /// <summary>
    /// Applies a style for the :pointerover pseudo-class.
    /// Example: buttonBuilder.SetPointerOverStyle((Button.BackgroundProperty, Brushes.LightGreen));
    /// </summary>
    /// <typeparam name="T">The type of Control.</typeparam>
    /// <param name="builder">The UI builder.</param>
    /// <param name="setters">A collection of property-value pairs to apply on hover.</param>
    /// <returns>The builder for method chaining.</returns>
    public static UIBuilder<T> SetPointerOverStyle<T>(
        this UIBuilder<T> builder,
        params (AvaloniaProperty Property, object Value)[] setters) where T : Control
    {
        // Selector: s => s.OfType<T>().Class(":pointerover")
        return builder.AddStyleInternal(s => s.OfType<T>().Class(":pointerover"), setters);
    }

    /// <summary>
    /// Applies a style for the :pressed pseudo-class.
    /// Example: buttonBuilder.SetPressedStyle((Button.BackgroundProperty, Brushes.DarkGreen));
    /// </summary>
    /// <typeparam name="T">The type of Control.</typeparam>
    /// <param name="builder">The UI builder.</param>
    /// <param name="setters">A collection of property-value pairs to apply when pressed.</param>
    /// <returns>The builder for method chaining.</returns>
    public static UIBuilder<T> SetPressedStyle<T>(
        this UIBuilder<T> builder,
        params (AvaloniaProperty Property, object Value)[] setters) where T : Control
    {
        // Selector: s => s.OfType<T>().Class(":pressed")
        return builder.AddStyleInternal(s => s.OfType<T>().Class(":pressed"), setters);
    }

    /// <summary>
    /// Applies a style for the :disabled pseudo-class.
    /// Example: buttonBuilder.SetDisabledStyle((Button.OpacityProperty, 0.5));
    /// </summary>
    /// <typeparam name="T">The type of Control.</typeparam>
    /// <param name="builder">The UI builder.</param>
    /// <param name="setters">A collection of property-value pairs to apply when disabled.</param>
    /// <returns>The builder for method chaining.</returns>
    public static UIBuilder<T> SetDisabledStyle<T>(
        this UIBuilder<T> builder,
        params (AvaloniaProperty Property, object Value)[] setters) where T : Control
    {
        // Selector: s => s.OfType<T>().Class(":disabled")
        return builder.AddStyleInternal(s => s.OfType<T>().Class(":disabled"), setters);
    }

    // Add more methods for other common pseudo-classes (:focus, etc.) or custom classes as needed...

    /// <summary>
    /// Applies a style for a specific pseudo-class or custom class directly on the control.
    /// NOTE: For state-dependent properties (Background, Foreground, BorderBrush on :pointerover, etc.),
    /// default theme styles might use more specific selectors targeting template parts, potentially overriding this style.
    /// Consider using SetClassTemplatePartStyle for common controls or inspecting the default theme.
    /// Example: buttonBuilder.SetClassStyle(":focus", (Button.BorderBrushProperty, Brushes.Blue));
    /// </summary>
    public static UIBuilder<T> SetClassStyle<T>(
        this UIBuilder<T> builder,
        string className, // e.g., ":pointerover", ":pressed", "custom-class"
        params (AvaloniaProperty Property, object Value)[] setters) where T : Control
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentException("Class name cannot be null or empty for SetClassStyle.", nameof(className));
        }
        // Selector: s => s.OfType<T>().Class(className)
        return builder.AddStyleInternal(s => s.OfType<T>().Class(className), setters);
    }

    /// <summary>
    /// Adds or removes a style class from the control.
    /// Example: builder.SetClass("highlighted", true); builder.SetClass("highlighted", false);
    /// </summary>
    /// <typeparam name="T">The type of Control.</typeparam>
    /// <param name="builder">The UI builder.</param>
    /// <param name="className">The class name to add or remove.</param>
    /// <param name="add">True to add the class, false to remove it.</param>
    /// <returns>The builder for method chaining.</returns>
    public static UIBuilder<T> SetClass<T>(this UIBuilder<T> builder, string className, bool add = true) where T : Control
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive())
            return builder;

        var control = builder.Get<Control>();
        if (add)
        {
            control.Classes.Add(className);
        }
        else
        {
            control.Classes.Remove(className);
        }
        return builder;
    }
    /// <summary>
    /// Applies a style targeting a specific named part within the control's template
    /// when a specific class (pseudo-class or custom) is active on the main control.
    /// This is often necessary to override default theme styles for states like :pointerover, :pressed.
    /// Example:
    /// buttonBuilder.SetClassTemplatePartStyle Button, ContentPresenter(
    ///     ":pointerover", // Pseudo-class on the Button
    ///     "PART_ContentPresenter", // Name of the part inside the template
    ///     (ContentPresenter.BackgroundProperty, Brushes.SkyBlue) // Property and Value for the ContentPresenter
    /// );
    /// </summary>
    /// <typeparam name="T">The type of the main Control being styled.</typeparam>
    /// <typeparam name="TPart">The type of the template part being targeted (e.g., ContentPresenter, Border).</typeparam>
    /// <param name="builder">The UI builder for the main control.</param>
    /// <param name="className">The class name on the main control (e.g., ":pointerover", "my-custom-class").</param>
    /// <param name="partName">The name of the template part (e.g., "PART_ContentPresenter"). Can be null if targeting by type only within template.</param>
    /// <param name="setters">A collection of property-value pairs to apply to the template part.</param>
    /// <returns>The builder for method chaining.</returns>
    public static UIBuilder<T> SetClassTemplatePartStyle<T, TPart>(
        this UIBuilder<T> builder,
        string className,
        string? partName, // Allow null for targeting only by type within template
        params (AvaloniaProperty Property, object Value)[] setters)
        where T : Control // Main control constraint
        where TPart : Control // Template part constraint
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentException("Class name cannot be null or empty.", nameof(className));
        }

        // Build the selector: T.className /template/ TPart#partName
        Func<Selector?, Selector> selectorBuilder = s =>
        {
            // Start with: T.className
            Selector finalSelector = s.OfType<T>().Class(className);
            // Add: /template/
            finalSelector = finalSelector.Template();
            // Add: TPart
            finalSelector = finalSelector.OfType<TPart>();
            // Add: #partName (if provided)
            if (!string.IsNullOrEmpty(partName))
            {
                finalSelector = finalSelector.Name(partName);
            }
            return finalSelector;
        };

        return builder.AddStyleInternal(selectorBuilder, setters); // Use the existing helper
    }

    /// <summary>
    /// Convenience method to set the controls's :pointerover background,
    /// correctly targeting the PART_ContentPresenter to override default themes.
    /// </summary>
    public static UIBuilder<T> SetPointerOverBackground<T>(this UIBuilder<T> builder, IBrush background) where T : Control
    {
        return builder.SetClassTemplatePartStyle<T, ContentPresenter>(
            ":pointerover",
            "PART_ContentPresenter", // Standard name in default themes
            (ContentPresenter.BackgroundProperty, background) // Target the part's property
        );
    }


    /// <summary>
    /// Convenience method to set the Button's :pressed background,
    /// correctly targeting the PART_ContentPresenter to override default themes.
    /// </summary>
    public static UIBuilder<Button> SetButtonPressedBackground(this UIBuilder<Button> builder, IBrush background)
    {
        return builder.SetClassTemplatePartStyle<Button, ContentPresenter>(
            ":pressed",
            "PART_ContentPresenter", // Standard name in default themes
            (ContentPresenter.BackgroundProperty, background) // Target the part's property
        );
    }
}

/// <summary>
/// A fluent builder for constructing hierarchical UI components as Flecs entities.
/// Provides a clean, nested syntax that visually represents the UI hierarchy. Events 
/// attached to ui builders are being disposed when the underlying entity of the ui
/// builder gets destroyed. There is no need to manaully remove event handlers from
/// avalonia objects when they are added that way.
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
    /// Provides direct access to the underlying control to configure its properties and methods,
    /// then returns the builder for continued method chaining.
    /// </summary>
    /// <param name="configure">Action to configure the control directly.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> With(Action<T> configure)
    {
        configure(_control);
        return this;
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
        return _entity.UI(configure);
    }

    /// <summary>
    /// Attaches an ui builder to another ui builder.
    /// </summary>
    /// <typeparam name="TChild"></typeparam>
    /// <param name="childBuilder"></param>
    /// <returns></returns>
    public UIBuilder<T> Child<TChild>(UIBuilder<TChild> childBuilder) where TChild : AvaloniaObject
    {
        childBuilder._entity.ChildOf(_entity);
        return this;
    }


    /// <summary>
    /// Attaches an ui component as a child. If they are implemented IDisposable
    /// its ensured that when the parent entity is destroyed it automatically calls
    /// the dispose method of its children.
    /// </summary>
    /// <param name="uIComponent"></param>
    /// <returns></returns>
    public UIBuilder<T> Child(IUIComponent uIComponent)
    {
        // 1. Attach the component's root entity as a child in the Flecs hierarchy.
        //    Assuming uIComponent.Attach() does something like uIComponent.Root.ChildOf(this.Entity)
        //    or ensures the UI hierarchy implies the Flecs hierarchy. If not, ensure
        //    uIComponent.Root is made a child of this.Entity here or within Attach.
        uIComponent.Attach(Entity); // Your existing logic

        // 2. Check if the component needs disposal and add the handle *to its own root*
        if (uIComponent is IDisposable disposableComponent)
        {
            // Ensure the component's root entity is valid and alive
            if (uIComponent.Root.IsValid() && uIComponent.Root.IsAlive())
            {
                uIComponent.Root.Set(new DisposableComponentHandle { Target = disposableComponent });
            }
        }
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
    /// Attaches an tag to an entity
    /// </summary>
    /// <returns></returns>
    public UIBuilder<T> Add<ComponentType>() where ComponentType : new()
    {
        Entity.Add<ComponentType>();
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