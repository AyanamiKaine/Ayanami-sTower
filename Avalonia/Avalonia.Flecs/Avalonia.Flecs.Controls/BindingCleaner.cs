using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.Flecs.Controls;

/// <summary>
/// Manages the lifecycle of data bindings and ensures proper cleanup when disposed.
/// </summary>
/// <param name="target">The Avalonia object that owns the binding.</param>
/// <param name="property">The property on the target object that has a binding.</param>
public class BindingCleaner(AvaloniaObject target, AvaloniaProperty property) : IDisposable
{
    private AvaloniaObject? _target = target;
    private AvaloniaProperty? _property = property;
    private bool _disposed = false;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed || _target == null || _property == null)
            return;

        // Ensure cleanup happens on the UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_target != null && _property != null)
            {
                // Check if the property still has a binding before clearing
                if (
                    _target.IsSet(_property)
                    && BindingOperations.GetBindingExpressionBase(_target, _property) != null
                )
                {
                    _target.ClearValue(_property);
                    //Console.WriteLine($"Cleared binding on {_target.GetType().Name} for property {_property.Name}");
                }
            }
            _target = null; // Release reference
            _property = null;
        });

        GC.SuppressFinalize(this);
        _disposed = true;
    }
}
