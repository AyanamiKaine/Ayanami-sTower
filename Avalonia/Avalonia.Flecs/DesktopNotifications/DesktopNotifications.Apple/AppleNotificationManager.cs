using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#pragma warning disable 0067

namespace DesktopNotifications.Apple
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class AppleNotificationManager : INotificationManager
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    {
        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public NotificationManagerCapabilities Capabilities => NotificationManagerCapabilities.None;

        /// <inheritdoc/>
        public event EventHandler<NotificationActivatedEventArgs>? NotificationActivated;
        /// <inheritdoc/>
        public event EventHandler<NotificationDismissedEventArgs>? NotificationDismissed;

        /// <inheritdoc/>
        public string? LaunchActionId { get; }

        /// <inheritdoc/>
        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ShowNotification(Notification notification, DateTimeOffset? expirationTime = null)
        {
            ShowNotification();

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ScheduleNotification(Notification notification, DateTimeOffset deliveryTime,
            DateTimeOffset? expirationTime = null)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task HideNotification(Notification notification)
        {
            return Task.CompletedTask;
        }

        [DllImport("DesktopNotifications.Apple.Native.dylib")]
        private static extern void ShowNotification();
    }
}