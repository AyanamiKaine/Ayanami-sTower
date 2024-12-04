using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{


    /// IMPORTANT
    /// ALL OBERSERVES RUN IN A NON-UI THREAD THIS IS THE DEFAULT BEHAVIOR IN AVALONIA
    /// ANY CODE EXECUTED IN AN OBSERVE THAT MODIFIES THE UI MUST BE DISPATCHED TO THE UI THREAD
    /// THIS CAN BE DONE BY USING THE 
    /// Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { /* UI CODE HERE */ });
    /// THIS ALSO MATTER FOR ALL FUNCTIONS 
    /// THAT WANT TO USE THE ECS WORLD FOUND IN MAIN THE APPLICATION

    public static class ECSEventExtensions
    {
        public static Entity RemoveEventHandler(this Entity entity, string eventName, Action<object, RoutedEventArgs> handler)
        {
            var control = entity.Get<Control>();
            var eventInfo = control.GetType().GetEvent(eventName);
            if (eventInfo != null)
            {
                eventInfo.RemoveEventHandler(control, handler);
            }
            else
            {
                throw new MissingMemberException($"Control of type {control.GetType()} does not have a {eventName} event");
            }

            return entity;
        }


        public static Entity OnClick(this Entity entity, Action<object?, RoutedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("Click");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<RoutedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a Click event");
            }

            return entity;
        }

        public static Entity OnTemplateApplied(this Entity entity, Action<object?, TemplateAppliedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("TemplateApplied");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<TemplateAppliedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a TemplateApplied event");
            }

            return entity;
        }

        public static Entity OnSelectionChanged(this Entity entity, Action<object?, SelectionChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("SelectionChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<SelectionChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a SelectionChanged event");
            }

            return entity;
        }

        public static Entity OnClosed(this Entity entity, Action<object?, EventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("Closed");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a Closed event");
            }

            return entity;
        }

        public static Entity OnOpened(this Entity entity, Action<object?, EventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("Opened");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a Opened event");
            }

            return entity;
        }

    }
}
