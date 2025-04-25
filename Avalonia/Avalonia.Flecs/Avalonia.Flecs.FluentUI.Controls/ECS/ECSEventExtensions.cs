using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Provides extension methods for attaching event handlers to FluentAvalonia controls associated with Flecs entities.
    /// </summary>
    public static class ECSEventExtensions
    {
        /// <summary>
        /// Attaches a handler to the BackRequested event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown when the object does not have a BackRequested event.</exception>
        public static Entity OnBackRequested(
            this Entity entity,
            Action<object?, NavigationViewBackRequestedEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("BackRequested");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewBackRequestedEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a BackRequested event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the ItemCollapsed event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown when the object does not have an ItemCollapsed event.</exception>
        public static Entity OnItemCollapsed(
            this Entity entity,
            Action<object?, NavigationViewItemCollapsedEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemCollapsed");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewItemCollapsedEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a ItemCollapsed event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the ItemExpanding event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown when the object does not have a ItemExpanding event.</exception>
        public static Entity OnItemExpanding(
            this Entity entity,
            Action<object?, NavigationViewItemExpandingEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemExpanding");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewItemExpandingEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a ItemExpanding event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the ItemInvoked event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown if the object does not have a ItemInvoked event.</exception>
        public static Entity OnItemInvoked(
            this Entity entity,
            Action<object?, NavigationViewItemInvokedEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemInvoked");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewItemInvokedEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a ItemInvoked event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the DisplayModeChanged event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown if the object does not have a DisplayModeChanged event.</exception>
        public static Entity OnDisplayModeChanged(
            this Entity entity,
            Action<object?, NavigationViewDisplayModeChangedEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DisplayModeChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewDisplayModeChangedEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a DisplayModeChanged event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the PaneClosing event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown if the object does not have a PaneClosing event.</exception>
        public static Entity OnPaneClosing(
            this Entity entity,
            Action<object?, NavigationViewPaneClosingEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PaneClosing");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewPaneClosingEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a PaneClosing event"
                );
            }

            return entity;
        }

        /// <summary>
        /// Attaches a handler to the SelectionChanged event of the object associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="MissingMemberException">Thrown if the object does not have a SelectionChanged event.</exception>
        public static Entity OnNavViewSelectionChanged(
            this Entity entity,
            Action<object?, NavigationViewSelectionChangedEventArgs> handler
        )
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("SelectionChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(
                    obj,
                    new EventHandler<NavigationViewSelectionChangedEventArgs>(handler)
                );
            }
            else
            {
                throw new MissingMemberException(
                    $"obj of type {obj.GetType()} does not have a SelectionChanged event"
                );
            }

            return entity;
        }
    }
}
