using System.Reflection;

namespace Avalonia.Flecs.Controls;

/// <summary>
/// Custom weak event handler class
/// </summary>
/// <typeparam name="TEventArgs"></typeparam>
public class WeakEventHandler<TEventArgs>
    where TEventArgs : EventArgs
{
    private readonly WeakReference _targetRef;
    private readonly MethodInfo _method;

    /// <summary>
    /// Custom weak event handler class
    /// </summary>
    /// <param name="callback"></param>
    public WeakEventHandler(EventHandler<TEventArgs> callback)
    {
        _targetRef = new WeakReference(callback.Target);
        _method = callback.Method;
    }

    /// <summary>
    /// s
    /// </summary>
    public EventHandler<TEventArgs> Handler
    {
        get
        {
            return (sender, e) =>
            {
                var target = _targetRef.Target;
                if (target != null)
                {
                    _method.Invoke(target, new object[] { sender!, e });
                }
            };
        }
    }
}
