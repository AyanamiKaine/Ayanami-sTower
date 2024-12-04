using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
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

        public static Entity OnAttachedToLogicalTree(this Entity entity, Action<object?, LogicalTreeAttachmentEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("AttachedToLogicalTree");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<LogicalTreeAttachmentEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a AttachedToLogicalTree event");
            }

            return entity;
        }

        public static Entity OnAttachedToVisualTree(this Entity entity, Action<object?, VisualTreeAttachmentEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("AttachedToVisualTree");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<VisualTreeAttachmentEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a AttachedToVisualTree event");
            }

            return entity;
        }

        public static Entity OnDataContextChanged(this Entity entity, Action<object?, EventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DataContextChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a DataContextChanged event");
            }

            return entity;
        }

        public static Entity OnDetachedFromLogicalTree(this Entity entity, Action<object?, LogicalTreeAttachmentEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DetachedFromLogicalTree");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<LogicalTreeAttachmentEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a DetachedFromLogicalTree event");
            }

            return entity;
        }

        public static Entity OnDetachedFromVisualTree(this Entity entity, Action<object?, VisualTreeAttachmentEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DetachedFromVisualTree");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<VisualTreeAttachmentEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a DetachedFromVisualTree event");
            }

            return entity;
        }

        public static Entity OnDoubleTapped(this Entity entity, Action<object?, RoutedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("DoubleTapped");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<RoutedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a DoubleTapped event");
            }

            return entity;
        }

        public static Entity OnEffectiveViewportChanged(this Entity entity, Action<object?, EffectiveViewportChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("EffectiveViewportChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EffectiveViewportChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a EffectiveViewportChanged event");
            }

            return entity;
        }

        public static Entity OnGotFocus(this Entity entity, Action<object?, GotFocusEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("GotFocus");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<GotFocusEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a GotFocus event");
            }

            return entity;
        }

        public static Entity OnInitialized(this Entity entity, Action<object?, EventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("Initialized");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a Initialized event");
            }

            return entity;
        }

        public static Entity OnKeyDown(this Entity entity, Action<object?, KeyEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("KeyDown");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<KeyEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a KeyDown event");
            }

            return entity;
        }

        public static Entity OnKeyUp(this Entity entity, Action<object?, KeyEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("KeyUp");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<KeyEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a KeyUp event");
            }

            return entity;
        }

        public static Entity OnLayoutUpdated(this Entity entity, Action<object?, EventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("LayoutUpdated");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<EventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a LayoutUpdated event");
            }

            return entity;
        }

        public static Entity OnLostFocus(this Entity entity, Action<object?, RoutedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("LostFocus");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<RoutedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a LostFocus event");
            }

            return entity;
        }

        public static Entity OnPointerCaptureLost(this Entity entity, Action<object?, PointerCaptureLostEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerCaptureLost");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerCaptureLostEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerCaptureLost event");
            }

            return entity;
        }

        public static Entity OnPointerEntered(this Entity entity, Action<object?, PointerEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerEntered");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerEntered event");
            }

            return entity;
        }

        public static Entity OnPointerExited(this Entity entity, Action<object?, PointerEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerExited");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerExited event");
            }

            return entity;
        }

        public static Entity OnPointerMoved(this Entity entity, Action<object?, PointerEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerMoved");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerMoved event");
            }

            return entity;
        }

        public static Entity OnPointerPressed(this Entity entity, Action<object?, PointerPressedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerPressed");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerPressedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerPressed event");
            }

            return entity;
        }

        public static Entity OnPointerReleased(this Entity entity, Action<object?, PointerReleasedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerReleased");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerReleasedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerReleased event");
            }

            return entity;
        }

        public static Entity OnPointerWheelChanged(this Entity entity, Action<object?, PointerWheelEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PointerWheelChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<PointerWheelEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PointerWheelChanged event");
            }

            return entity;
        }

        public static Entity OnPropertyChanged(this Entity entity, Action<object?, AvaloniaPropertyChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("PropertyChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<AvaloniaPropertyChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a PropertyChanged event");
            }

            return entity;
        }

        public static Entity OnResourceChanged(this Entity entity, Action<object?, ResourcesChangedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("ResourceChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<ResourcesChangedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a ResourceChanged event");
            }

            return entity;
        }

        public static Entity OnTapped(this Entity entity, Action<object?, TappedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("Tapped");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<TappedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a Tapped event");
            }

            return entity;
        }

        public static Entity OnTextInput(this Entity entity, Action<object?, TextInputEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("TextInput");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<TextInputEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a TextInput event");
            }

            return entity;
        }

        public static Entity OnTextInputMethodClientRequested(this Entity entity, Action<object?, TextInputMethodClientRequestedEventArgs> handler)
        {
            var obj = entity.Get<object>();
            var eventInfo = obj.GetType().GetEvent("TextInputMethodClientRequested");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(obj, new EventHandler<TextInputMethodClientRequestedEventArgs>(handler));
            }
            else
            {
                throw new MissingMemberException($"obj of type {obj.GetType()} does not have a TextInputMethodClientRequested event");
            }

            return entity;
        }

    }
}
