using Avalonia.Controls;

namespace Avalonia.Flecs.Controls
{
    /// <summary>
    /// Extension methods for Control components.
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Helper for setting Row property on a Control component for an entity.
        /// </summary>
        public static Control SetRow(this Control control, int value)
        {
            Grid.SetRow(control, value);
            return control;
        }
        /// <summary>
        /// Helper for setting ColumnSpan property on a Control component for an entity.
        /// </summary>
        public static Control SetColumnSpan(this Control control, int value)
        {
            Grid.SetColumnSpan(control, value);
            return control;
        }

        /// <summary>
        /// Helper for setting Column property on a Control component for an entity.
        /// </summary>
        public static Control SetColumn(this Control control, int value)
        {
            Grid.SetColumn(control, value);
            return control;
        }

        /// <summary>
        /// Helper for setting RowSpan property on a Control component for an entity.
        /// </summary>
        public static Control SetRowSpan(this Control control, int value)
        {
            Grid.SetRowSpan(control, value);
            return control;
        }
    }
}
