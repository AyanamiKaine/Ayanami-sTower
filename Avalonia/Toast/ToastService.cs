using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

namespace AyanamisTower.Toast;

/// <summary>
/// Central place for toast to be spawned from
/// </summary>
public static class ToastService
{
    private static Panel? _notificationHost;

    /// <summary>
    /// Initialize the service with the host panel where toasts will be displayed
    /// </summary>
    /// <param name="notificationHost"></param>
    public static void Initialize(Panel notificationHost)
    {
        _notificationHost = notificationHost;
    }

    /// <summary>
    /// Show a toast notification
    /// </summary>
    /// <param name="message"></param>
    /// <param name="type"></param>
    /// <param name="duration"></param>
    public static void Show(string message,
                            NotificationType type = NotificationType.Information,
                            TimeSpan? duration = null)
    {
        if (_notificationHost == null)
        {
            // Consider logging this or throwing a more specific exception
            // For now, we'll write to debug output.
            System.Diagnostics.Debug.WriteLine("ToastService not initialized. Call Initialize() first.");
            return;
        }

        // Ensure execution on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            var toast = new ToastControl
            {
                Message = message,
                Type = type,
            };

            if (duration.HasValue)
            {
                toast.Duration = duration.Value;
            }

            // Add the toast to the host panel
            // If using a StackPanel, toasts will stack. If Canvas, you might need to manage positioning.
            _notificationHost.Children.Add(toast);
        });
    }

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="type">The type of notification (Info, Success, Warning, Error).</param>
    public static ToastControl Show(string message,
                                    NotificationType type = NotificationType.Information)
    {
        if (_notificationHost == null)
        {
            throw new Exception("ToastService not initialized. Call Initialize(Panel host) first.");
        }


        // Dispatcher.UIThread.Post might not be ideal if we need to return the instance immediately.
        // Let's use InvokeAsync if we need to return, or ensure Show is called from UI thread.
        // For simplicity and common use cases, creating on UI thread and returning is fine.
        // If Show can be called from non-UI thread and needs to return immediately,
        // the creation and addition to host must be marshalled.

        // Assuming Show will be called from a context that can wait or is already UI thread.
        // If not, the caller would need to handle the async nature of UI updates.
        // For returning the toast instance synchronously, it's best if this method is called from the UI thread.
        // If called from background, the caller would get the instance before it's fully added to visual tree.
        // However, the instance itself is created synchronously.

        ToastControl toast = new()
        {
            Message = message,
            Type = type,
        }; // Declare here to return it


        // Add to host panel on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _notificationHost.Children.Add(toast);
        });

        return toast; // Return the created toast instance
    }
}