namespace DesktopNotifications
{
    /// <summary>
    /// Provides data for the notification dismissed event.
    /// </summary>
    public class NotificationDismissedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDismissedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification that was dismissed.</param>
        /// <param name="reason">The reason why the notification was dismissed.</param>
        public NotificationDismissedEventArgs(Notification notification, NotificationDismissReason reason)
        {
            Notification = notification;
            Reason = reason;
        }

        /// <summary>
        /// Gets the notification that was dismissed.
        /// </summary>
        public Notification Notification { get; }

        /// <summary>
        /// Gets the reason why the notification was dismissed.
        /// </summary>
        public NotificationDismissReason Reason { get; }
    }
}