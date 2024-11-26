using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
namespace Avalonia.Flecs.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {

        ////RELATIONSHIPS
        public struct InnerRightContent { };
        public struct InnerLeftContent { };
        public struct KeyBindings { };

        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();
            RegisterEventDataComponents(world);
            world.Import<ECSButton>();
            world.Import<ECSAutoCompleteBox>();
            world.Import<ECSCanvas>();
            world.Import<ECSContentControl>();
            world.Import<ECSItemsControl>();
            world.Import<ECSControl>();
            world.Import<ECSDockPanel>();
            world.Import<ECSGrid>();
            world.Import<ECSItemsControl>();
            world.Import<ECSPanel>();
            world.Import<ECSStackPanel>();
            world.Import<ECSWrapPanel>();
            world.Import<ECSWindow>();
            world.Import<ECSRelativePanel>();
            world.Import<ECSStackPanel>();
            world.Import<ECSScrollViewer>();
            world.Import<ECSTemplatedControl>();
            world.Import<ECSTextBlock>();
            world.Import<ECSTextBox>();
            world.Import<ECSComboBox>();
            world.Import<ECSWindow>();


            //This Observer handles the functionality adding entity as children of other
            //and correctly adding the control element to the parent event if the parent
            //child relation was created later than the component control where attached.
            world.Observer("ControlToParentAdder")
                .Event(Ecs.OnAdd)
                .Event(Ecs.OnSet)
                .With(Ecs.ChildOf, Ecs.Wildcard)
                .Each((Entity child) =>
                {
                    var parent = child.Parent();

                    if (child.Has<Control>())
                    {
                        var control = child.Get<Control>();
                        if (parent.Has<Panel>())
                        {
                            //We dont want to add the control twice,
                            //This otherwise throws an exception
                            if (parent.Get<Panel>().Children.Contains(control))
                            {
                                return;
                            }
                            parent.Get<Panel>().Children.Add(control);
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = control;
                        }
                    }
                    Console.WriteLine($"Added child: {child.Name()} to parent: {child.Parent().Name()}");
                });
        }
        public static void RegisterEventDataComponents(World world)
        {
            world.Component<Click>("Click");
            world.Component<Activated>("Activated");
            world.Component<AttachedToLogicalTree>("AttachedToLogicalTree");
            world.Component<AttachedToVisualTree>("AttachedToVisualTree");
            world.Component<Closed>("Closed");
            world.Component<Closing>("Closing");
            world.Component<DataContextChanged>("DataContextChanged");
            world.Component<Deactivated>("Deactivated");
            world.Component<DetachedFromLogicalTree>("DetachedFromLogicalTree");
            world.Component<DetachedFromVisualTree>("DetachedFromVisualTree");
            world.Component<DoubleTapped>("DoubleTapped");
            world.Component<EffectiveViewportChanged>("EffectiveViewportChanged");
            world.Component<GotFocus>("GotFocus");
            world.Component<Initialized>("Initialized");
            world.Component<KeyDown>("KeyDown");
            world.Component<KeyUp>("KeyUp");
            world.Component<LayoutUpdated>("LayoutUpdated");
            world.Component<LostFocus>("LostFocus");
            world.Component<Opened>("Opened");
            world.Component<PointerCaptureLost>("PointerCaptureLost");
            world.Component<PointerEnter>("PointerEnter");
            world.Component<PointerLeave>("PointerLeave");
            world.Component<PointerMoved>("PointerMoved");
            world.Component<PointerPressed>("PointerPressed");
            world.Component<PointerReleased>("PointerReleased");
            world.Component<PointerWheelChanged>("PointerWheelChanged");
            world.Component<PositionChanged>("PositionChanged");
            world.Component<PropertyChanged>("PropertyChanged");
            world.Component<ResourcesChanged>("ResourcesChanged");
            world.Component<Tapped>("Tapped");
            world.Component<TemplateApplied>("TemplateApplied");
            world.Component<TextInput>("TextInput");
            world.Component<TextInputMethodClientRequested>("TextInputMethodClientRequested");
        }
    }
}