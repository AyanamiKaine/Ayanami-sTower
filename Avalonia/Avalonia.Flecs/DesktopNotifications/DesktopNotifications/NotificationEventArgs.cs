namespace DesktopNotifications
{
    /// <summary>
    /// Provides data for notification events.
    /// </summary>
    public class NotificationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification associated with the event.</param>
        public NotificationEventArgs(Notification notification)
        {
            Notification = notification;
        }

        /// <summary>
        /// Gets the notification associated with the event.
        /// </summary>
        public Notification Notification { get; }
    }
}