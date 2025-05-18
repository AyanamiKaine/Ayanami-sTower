using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Notifications; // For NotificationType
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AyanamisTower.Toast;


/*
TODO:
I think we should add events for 
OnDismissed, OnStarted etc.

The dismissed event is important because so we can add hooks like, abort operation 
when the user dismissed the toast.
*/

/// <summary>
/// A control that displays toast notifications in an Avalonia application.
/// Supports click-to-dismiss and timed auto-dismissal with animations.
/// </summary>
public class ToastControl : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="Message"/> property.
    /// </summary>
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ToastControl, string>(nameof(Message), "Default Message");

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Type"/> property.
    /// </summary>
    public static readonly StyledProperty<NotificationType> TypeProperty =
        AvaloniaProperty.Register<ToastControl, NotificationType>(nameof(Type), NotificationType.Information);

    /// <summary>
    /// Gets or sets the type of the notification.
    /// </summary>
    public NotificationType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Duration"/> property.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<ToastControl, TimeSpan>(nameof(Duration), TimeSpan.FromMinutes(1)); // Default duration increased slightly

    /// <summary>
    /// Gets or sets the duration for which the toast notification is visible before auto-dismissing.
    /// </summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private Border? _visualRoot; // The main container (Border)
    private TextBlock? _messageTextBlock; // The TextBlock for the message

    private CancellationTokenSource? _dismissCts;
    private bool _isDismissing;

    // Define animation durations (can be customized)
    private static readonly TimeSpan FadeInAnimationDuration = TimeSpan.FromSeconds(0.3);
    private static readonly TimeSpan FadeOutAnimationDuration = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastControl"/> class.
    /// </summary>
    public ToastControl()
    {
        // Define default visual properties for the control itself
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0, 0, 0, 20); // Margin from the bottom of its container
        Padding = new Thickness(15, 10);    // This padding will be used by the internal Border
        MinWidth = 200;
        MaxWidth = 400;
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        // RenderTransform = new TranslateTransform(); // Initialized for animations if needed, though current animations only use Opacity

        InitializeVisualTree();
        InitializeStyles(); // Define and add styles (including animations) programmatically
    }

    private void InitializeVisualTree()
    {
        _messageTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White, // Default text color
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Bind TextBlock.TextProperty to this.MessageProperty
        _messageTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(Message)) { Source = this });

        _visualRoot = new Border
        {
            CornerRadius = new CornerRadius(5),
            BoxShadow = new BoxShadows(new BoxShadow { OffsetX = 0, OffsetY = 2, Blur = 6, Spread = -1, Color = Color.FromArgb(64, 0, 0, 0) }), // Subtle shadow
            Child = _messageTextBlock
        };

        // Bind Border.BackgroundProperty to this.BackgroundProperty (which we set in UpdateBackgroundColor)
        _visualRoot.Bind(Border.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
        // Bind Border.PaddingProperty to this.PaddingProperty
        _visualRoot.Bind(Decorator.PaddingProperty, new Binding(nameof(Padding)) { Source = this });

        // Add click handler for dismissal
        _visualRoot.PointerPressed += OnPointerPressedToDismiss;


        Template = new FuncControlTemplate<ToastControl>((control, scope) => _visualRoot);
    }

    private void InitializeStyles()
    {
        // Fade-in animation style
        var fadeInStyle = new Style(x => x.OfType<ToastControl>().Class("fadeIn"));
        var fadeInAnimation = new Animation
        {
            Duration = FadeInAnimationDuration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(OpacityProperty, 1.0) }
                }
            }
        };
        fadeInStyle.Animations.Add(fadeInAnimation);
        Styles.Add(fadeInStyle);

        // Fade-out animation style
        var fadeOutStyle = new Style(x => x.OfType<ToastControl>().Class("fadeOut"));
        var fadeOutAnimation = new Animation
        {
            Duration = FadeOutAnimationDuration,
            Easing = new CubicEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(OpacityProperty, 0.0) }
                }
            }
        };
        fadeOutStyle.Animations.Add(fadeOutAnimation);
        Styles.Add(fadeOutStyle);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // Visual elements are already referenced from InitializeVisualTree
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Start the lifecycle (fade-in and timer for auto-dismiss)
        // Ensure this is posted to the UI thread to avoid issues if attached from non-UI thread.
        Dispatcher.UIThread.Post(async () => await StartLifecycleAsync());
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        // Clean up resources if the control is removed unexpectedly
        if (!_isDismissing)
        {
            _dismissCts?.Cancel();
        }
        _dismissCts?.Dispose();
        _dismissCts = null;

        if (_visualRoot != null)
        {
            _visualRoot.PointerPressed -= OnPointerPressedToDismiss;
        }
    }

    private async void OnPointerPressedToDismiss(object? sender, PointerPressedEventArgs e)
    {
        // Optional: Check for primary button if needed, e.g., e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
        e.Handled = true; // Mark event as handled to prevent further processing if necessary
        await DismissAsync();
    }

    private async Task StartLifecycleAsync()
    {
        if (_isDismissing) return; // Already in the process of dismissing

        UpdateBackgroundColor(); // Set initial background based on Type

        Classes.Add("fadeIn"); // Trigger fade-in animation

        // Allow fade-in animation to play
        // Not strictly necessary to wait if Duration is significantly longer than FadeInAnimationDuration
        // await Task.Delay(FadeInAnimationDuration); 

        _dismissCts = new CancellationTokenSource();
        try
        {
            // Wait for the specified duration or until dismissal is triggered
            await Task.Delay(Duration, _dismissCts.Token);

            // If Task.Delay completes without cancellation, the duration has expired
            // Proceed with automatic dismissal if not already dismissing
            if (!_isDismissing)
            {
                await DismissAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected if DismissAsync() was called externally (e.g., by click),
            // cancelling the Task.Delay. The DismissAsync method handles the rest.
            System.Diagnostics.Debug.WriteLine("Toast duration delay was canceled, likely by manual dismissal.");
        }
        catch (Exception ex)
        {
            // Log any other unexpected errors during the lifecycle
            System.Diagnostics.Debug.WriteLine($"Error in ToastControl lifecycle: {ex.Message}");
            // Ensure cleanup even if an error occurs
            if (!_isDismissing) await DismissAsync(); // Attempt to dismiss to clean up
        }
        // CancellationTokenSource is disposed in DismissAsync or OnDetachedFromVisualTree
    }

    /// <summary>
    /// Programmatically dismisses the toast notification with an animation.
    /// </summary>
    public async Task DismissAsync()
    {
        // Ensure operations are on the UI thread if there's any doubt about the caller's context.
        // However, pointer events and Dispatcher.Post from OnAttachedToVisualTree should ensure this.
        // If called from an arbitrary thread, marshalling would be needed:
        // if (!Dispatcher.UIThread.CheckAccess()) { await Dispatcher.UIThread.InvokeAsync(DismissAsync); return; }

        if (_isDismissing)
        {
            return; // Already dismissing, do nothing further.
        }
        _isDismissing = true;

        _dismissCts?.Cancel(); // Cancel any ongoing Duration delay.

        // Switch classes to trigger fade-out animation
        Classes.Remove("fadeIn");
        Classes.Add("fadeOut");

        // Wait for the fade-out animation to complete
        await Task.Delay(FadeOutAnimationDuration);

        // Remove the control from its parent in the visual tree
        if (Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(this);
        }

        // Clean up the CancellationTokenSource
        _dismissCts?.Dispose();
        _dismissCts = null;
    }

    private void UpdateBackgroundColor()
    {
        // This directly sets the BackgroundProperty of the ToastControl itself.
        // The _visualRoot Border's Background is bound to this property.
        Background = Type switch
        {
            NotificationType.Information => Brushes.CornflowerBlue,
            NotificationType.Success => Brushes.SeaGreen,
            NotificationType.Warning => Brushes.Orange,
            NotificationType.Error => Brushes.Crimson,
            _ => Brushes.SlateGray, // Default color
        };
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TypeProperty)
        {
            // If the Type changes while the toast is visible, update its background
            if (this.IsAttachedToVisualTree()) // Check if it's active
            {
                UpdateBackgroundColor();
            }
        }
        // MessageProperty changes are handled by the C# binding to _messageTextBlock.TextProperty
        // BackgroundProperty changes (from UpdateBackgroundColor) are handled by C# binding to _visualRoot.BackgroundProperty
    }
}
