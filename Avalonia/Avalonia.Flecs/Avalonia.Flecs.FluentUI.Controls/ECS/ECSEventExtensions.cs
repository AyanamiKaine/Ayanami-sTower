using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public static class ECSEventExtensions
    {
        public static Entity OnBackRequested(this Entity entity, Action<object?, NavigationViewBackRequestedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("BackRequested");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewBackRequestedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a BackRequested event");
            }

            return entity;
        }

        public static Entity OnItemCollapsed(this Entity entity, Action<object?, NavigationViewItemCollapsedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemCollapsed");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewItemCollapsedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a ItemCollapsed event");
            }

            return entity;
        }

        public static Entity OnItemExpanding(this Entity entity, Action<object?, NavigationViewItemExpandingEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemExpanding");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewItemExpandingEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a ItemExpanding event");
            }

            return entity;
        }

        public static Entity OnItemInvoked(this Entity entity, Action<object?, NavigationViewItemInvokedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ItemInvoked");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewItemInvokedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a ItemInvoked event");
            }

            return entity;
        }

        public static Entity OnDisplayModeChanged(this Entity entity, Action<object?, NavigationViewDisplayModeChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DisplayModeChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewDisplayModeChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a DisplayModeChanged event");
            }

            return entity;
        }

        public static Entity OnPaneClosing(this Entity entity, Action<object?, NavigationViewPaneClosingEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PaneClosing");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewPaneClosingEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PaneClosing event");
            }

            return entity;
        }

        public static Entity OnNavViewSelectionChanged(this Entity entity, Action<object?, NavigationViewSelectionChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("SelectionChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<NavigationViewSelectionChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a SelectionChanged event");
            }

            return entity;
        }
    }
}