namespace DesktopNotifications
{
    /// <summary>
    /// Application Context
    /// </summary>
    public class ApplicationContext
    {
        /// <summary>
        /// Context Constructor
        /// </summary>
        /// <param name="name"></param>
        public ApplicationContext(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the Name
        /// </summary>
        public string Name { get; }
    }
}