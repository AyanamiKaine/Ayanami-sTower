using System;

namespace DesktopNotifications
{
    /// <summary>
    /// Specifies the capabilities of a notification manager.
    /// </summary>
    /// <remarks>
    /// This enumeration uses the <see cref="FlagsAttribute"/>, which allows a bitwise combination of its member values.
    /// </remarks>
    [Flags]
    public enum NotificationManagerCapabilities
    {
        /// <summary>
        /// The notification manager has no capabilities.
        /// </summary>
        None = 0,
        /// <summary>
        /// The notification manager supports body text.
        /// </summary>
        BodyText = 1 << 0,
        /// <summary>
        /// The notification manager supports body images.
        /// </summary>
        BodyImages = 1 << 1,
        /// <summary>
        /// The notification manager supports body markup.
        /// </summary>
        BodyMarkup = 1 << 2,
        /// <summary>
        /// The notification manager supports audio.
        /// </summary>
        Audio = 1 << 3,
        /// <summary>
        /// The notification manager supports icon.
        /// </summary>
        Icon = 1 << 4
    }
}