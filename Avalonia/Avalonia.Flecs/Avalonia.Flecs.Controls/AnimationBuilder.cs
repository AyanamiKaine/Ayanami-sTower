using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

namespace Avalonia.Flecs.Controls;



/*

Example usage for an animation on a button

button
.OnPointerEntered(async (_, _) =>
{
    var hoverOpacityAnimation = new AnimationBuilder()
        .WithDuration(TimeSpan.FromMilliseconds(200))
        .WithEasing(new CubicEaseInOut())
        .AddKeyFrame(1.0, Visual.OpacityProperty, 0.1)
        .WithFillMode(FillMode.Forward)        // Keep the final state
        .Build();

    await hoverOpacityAnimation.RunAsync(startLearningButton.Get<Button>(), CancellationToken.None);
})
.OnPointerExited(async (_, _) =>
{
    var hoverOpacityAnimation = new AnimationBuilder()
        .WithDuration(TimeSpan.FromMilliseconds(200))
        .WithEasing(new CubicEaseInOut())
        .AddKeyFrame(1.0, Visual.OpacityProperty, 1.0)
        .WithFillMode(FillMode.Forward)        // Keep the final state
        .Build();

    await hoverOpacityAnimation.RunAsync(startLearningButton.Get<Button>(), CancellationToken.None);
})
*/

/// <summary>
/// A builder class for creating Avalonia Animations in a fluent way.
/// </summary>
public class AnimationBuilder
{
    private TimeSpan _duration = TimeSpan.FromSeconds(1); // Default duration
    private Easing _easing = new LinearEasing();      // Default easing
    private readonly List<KeyFrame> _keyFrames = [];
    private IterationCount _iterationCount = new(1); // Default to 1 iteration
    private PlaybackDirection _playbackDirection = PlaybackDirection.Normal;
    private FillMode _fillMode = FillMode.None;

    /// <summary>
    /// Sets the duration of the animation.
    /// </summary>
    public AnimationBuilder WithDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    /// <summary>
    /// Sets the easing function for the animation.
    /// </summary>
    public AnimationBuilder WithEasing(Easing easing)
    {
        _easing = easing ?? new LinearEasing(); // Ensure easing is not null
        return this;
    }

    /// <summary>
    /// Adds a keyframe to the animation.
    /// </summary>
    /// <param name="cue">The point in time for this keyframe.</param>
    /// <param name="property">The AvaloniaProperty to animate.</param>
    /// <param name="value">The target value for the property at this cue.</param>
    public AnimationBuilder AddKeyFrame(Cue cue, AvaloniaProperty property, object value)
    {
        _keyFrames.Add(new KeyFrame { Cue = cue, Setters = { new Setter(property, value) } });
        return this;
    }

    /// <summary>
    /// Adds a keyframe to the animation using a double for cue time (0.0 to 1.0).
    /// </summary>
    /// <param name="cueTime">The point in time for this keyframe, from 0.0 (start) to 1.0 (end).</param>
    /// <param name="property">The AvaloniaProperty to animate.</param>
    /// <param name="value">The target value for the property at this cue.</param>
    public AnimationBuilder AddKeyFrame(double cueTime, AvaloniaProperty property, object value)
    {
        if (cueTime < 0.0 || cueTime > 1.0)
            throw new ArgumentOutOfRangeException(nameof(cueTime), "Cue time must be between 0.0 and 1.0.");
        return AddKeyFrame(new Cue(cueTime), property, value);
    }

    /// <summary>
    /// Adds a keyframe to the animation for multiple properties at the same cue.
    /// </summary>
    /// <param name="cue">The point in time for this keyframe.</param>
    /// <param name="setters">An array of tuples, each containing an AvaloniaProperty and its target value.</param>
    public AnimationBuilder AddKeyFrame(Cue cue, params (AvaloniaProperty Property, object Value)[] setters)
    {
        var keyFrame = new KeyFrame { Cue = cue };
        foreach (var (property, value) in setters)
        {
            keyFrame.Setters.Add(new Setter(property, value));
        }
        _keyFrames.Add(keyFrame);
        return this;
    }

    /// <summary>
    /// Adds a keyframe to the animation for multiple properties at the same cue, using a double for cue time (0.0 to 1.0).
    /// </summary>
    /// <param name="cueTime">The point in time for this keyframe, from 0.0 (start) to 1.0 (end).</param>
    /// <param name="setters">An array of tuples, each containing an AvaloniaProperty and its target value.</param>
    public AnimationBuilder AddKeyFrame(double cueTime, params (AvaloniaProperty Property, object Value)[] setters)
    {
        if (cueTime < 0.0 || cueTime > 1.0)
            throw new ArgumentOutOfRangeException(nameof(cueTime), "Cue time must be between 0.0 and 1.0.");
        return AddKeyFrame(new Cue(cueTime), setters);
    }

    /// <summary>
    /// Sets the iteration count for the animation.
    /// </summary>
    public AnimationBuilder WithIterationCount(IterationCount iterationCount)
    {
        _iterationCount = iterationCount;
        return this;
    }

    /// <summary>
    /// Sets the playback direction for the animation.
    /// </summary>
    public AnimationBuilder WithPlaybackDirection(PlaybackDirection playbackDirection)
    {
        _playbackDirection = playbackDirection;
        return this;
    }

    /// <summary>
    /// Sets the fill mode for the animation (e.g., whether it holds its final state).
    /// </summary>
    public AnimationBuilder WithFillMode(FillMode fillMode)
    {
        _fillMode = fillMode;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured Animation object.
    /// </summary>
    public Animation.Animation Build()
    {
        var animation = new Animation.Animation
        {
            Duration = _duration,
            Easing = _easing,
            IterationCount = _iterationCount,
            PlaybackDirection = _playbackDirection,
            FillMode = _fillMode
        };

        foreach (var keyFrame in _keyFrames)
        {
            animation.Children.Add(keyFrame);
        }

        // Clear keyframes for potential reuse of the builder instance, though typically new builder per animation is cleaner.
        // _keyFrames.Clear(); 

        return animation;
    }

    /// <summary>
    /// Builds the animation and runs it on the specified target.
    /// </summary>
    /// <param name="target">The control or animatable object to run the animation on.</param>
    /// <param name="cancellationToken">An optional cancellation token. If no is given it uses CancellationToken.None by default</param>
    /// <returns>A task that completes when the animation finishes.</returns>
    public Task BuildAndRunAsync(Animatable target, CancellationToken? cancellationToken = null)
    {
        var animation = Build();
        return animation.RunAsync(target, cancellationToken ?? CancellationToken.None);
    }
}