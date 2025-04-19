namespace DesktopNotifications
{
    /// <summary>
    /// Provides data for the notification activated event.
    /// </summary>
    public class NotificationActivatedEventArgs : NotificationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationActivatedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification that was activated.</param>
        /// <param name="actionId">The ID of the action that was invoked.</param>
        public NotificationActivatedEventArgs(Notification notification, string actionId)
            : base(notification)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// The id associated with the activation action. "default" denotes the platform-specific default action.
        /// On Windows this means the user clicked on the notification.
        /// </summary>
        public string ActionId { get; }
    }
}