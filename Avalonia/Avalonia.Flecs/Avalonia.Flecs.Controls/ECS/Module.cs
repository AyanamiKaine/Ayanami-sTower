using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {

        ////RELATIONSHIPS
        public struct InnerRightContent { };
        public struct InnerLeftContent { };
        public struct KeyBindings { };
        /// <summary>
        /// Entity tag showing that the 
        /// entity is a page. A page represents
        /// a root component in the UI hierarchy.
        /// Only one page entity can be a child of a parent.
        /// If you add a new page to a parent othe page of the parent is removed.
        /// </summary>
        public struct Page { }
        /// <summary>
        /// Inidactes that the entity is a page that is being removed.
        /// </summary>
        public struct OldPage { }
        /// <summary>
        /// Inidactes that the entity is a page that is being added.
        /// </summary>
        public struct NewPage { }
        /// <summary>
        /// Entity tag showing that the entity is the current page.
        /// </summary>
        public record struct CurrentPage { }


        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();
            RegisterEventDataComponents(world);

            world.Import<ECSInputElement>();
            world.Import<ECSInteractive>();
            world.Import<ECSLayoutable>();

            world.Import<ECSPopupFlyoutBase>();
            world.Import<ECSFlyoutBase>();
            world.Import<ECSMenuFlyout>();
            world.Import<ECSMenuItem>();
            world.Import<ECSMenu>();

            world.Import<ECSToggleButton>();
            world.Import<ECSButton>();
            world.Import<ECSRepeatButton>();
            world.Import<ECSRadioButton>();
            world.Import<ECSSplitButton>();
            world.Import<ECSToggleSplitButton>();
            world.Import<ECSToggleSwitch>();
            world.Import<ECSBorder>();
            world.Import<ECSExpander>();
            world.Import<ECSHeaderedContentControl>();
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
            world.Import<ECSSelectingItemsControl>();
            world.Import<ECSListBox>();


            AddUIComponentTags(world);
            AddControlToParentAdderObserver(world);
            AddFlyoutToControlObserver(world);
            //AddPageObserver(world);

        }

        public static void AddUIComponentTags(World world)
        {
            world.Component<Page>("Page");
            world.Component<CurrentPage>("CurrentPage")
                .Entity.Add(Ecs.Exclusive)
                .Add(Ecs.Relationship);
        }

        /// <summary>
        /// This observer adds the flyout to the control
        /// when a child has the flyout component and 
        /// is attached to a parent control.
        /// </summary>
        /// <param name="world"></param>
        public static void AddFlyoutToControlObserver(World world)
        {
            world.Observer("FlyoutToControl")
                .Event(Ecs.OnAdd)
                .Event(Ecs.OnSet)
                .With(Ecs.ChildOf, Ecs.Wildcard)
                .Each((Entity child) =>
                {
                    if (child.Has<FlyoutBase>())
                    {
                        var parent = child.Parent();
                        if (parent.Has<Control>())
                        {
                            parent.Get<Control>().ContextFlyout = child.Get<FlyoutBase>();
                        }
                    }
                });
        }

        public static void AddControlToParentAdderObserver(World world)
        {
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
                    //Console.WriteLine($"Added child: {child.Name()} to parent: {child.Parent().Name()}");
                });
        }


        /*
        Design NOTE:

        Maybe we want to refactor this. Here we have the idea of a page. A hierarchy of entities that represent something more 
        similar to a react component. The thing is that in react we have a single root component that is the page. 

        So a page is more of a root component. So maybe calling it differently would be better.

        We have a conceptual overlap and a mental model mismatch. 
        */
        public static void AddPageObserver(World world)
        {
            /*
            We use this system to ensure that a parent has only one child that is a page.
            This is useful when we want to change the page displayed in a control.

            SADLY FOR NOW ITS BUGGED:
            We cannot count on the order of the entities in the list. Sometimes the first page added
            is not the first child ecounterd with the page tag !
            */
            world.Observer("EnsureEntityHasOnlyOnePageChild")
                .Event(Ecs.OnAdd)
                .Event(Ecs.OnSet)
                .With(Ecs.ChildOf, Ecs.Wildcard)
                .Each((Entity child) =>
                {
                    var parent = child.Parent();
                    //When we select a new page to display we remove 
                    //all currently attached childrens that are pages
                    //So we can only have one page displayed at a time.
                    //so the ControlToParentAdder observer runs.

                    if (parent == 0)
                    {
                        return;
                    }

                    if (child.Has<Page>())
                    {

                        List<Entity> pages = [];
                        parent.Children((Entity child) =>
                        {
                            if (child.Has<Page>())
                            {
                                pages.Add(child);
                            }
                            if (pages.Count > 1)
                            {
                                // This does not work because we cannot count on the order
                                // of the entities in the list. Sometimes the first page added 
                                // is not the first child ecounterd with the page tag !
                                //pages[0].Remove(Ecs.ChildOf, Ecs.Wildcard);
                            }
                        });
                    }
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
            world.Component<TextChanging>("TextChanging");
            world.Component<TextChanged>("TextChanged");
            world.Component<TextInputMethodClientRequested>("TextInputMethodClientRequested");
        }
    }
}