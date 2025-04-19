using System.Collections.Generic;

namespace DesktopNotifications
{
    /// <summary>
    /// Represents a desktop notification with a title, body, optional image, and action buttons.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        public Notification()
        {
            Buttons = new List<(string Title, string ActionId)>();
        }

        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the main text content (body) of the notification.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the path to an image to be displayed within the notification body.
        /// </summary>
        /// <remarks>
        /// The image path should be accessible by the notification system (e.g., a local file path or a URI).
        /// </remarks>
        public string? BodyImagePath { get; set; }

        /// <summary>
        /// Gets or sets the alternative text for the body image, used for accessibility purposes.
        /// Defaults to "Image".
        /// </summary>
        public string BodyImageAltText { get; set; } = "Image";

        /// <summary>
        /// Gets the list of buttons to be displayed on the notification.
        /// Each button is represented by a tuple containing its display <c>Title</c> and an <c>ActionId</c>
        /// used to identify the action when the button is clicked.
        /// </summary>
        public List<(string Title, string ActionId)> Buttons { get; }
    }
}