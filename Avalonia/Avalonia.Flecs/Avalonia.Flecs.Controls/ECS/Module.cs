using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Controls.Primitives;
using NLog;
namespace Avalonia.Flecs.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    /// <summary>
    /// This ECS Module is used to register the Control component
    /// </summary>
    public struct Module : IFlecsModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        ////RELATIONSHIPS
        /// <summary>
        /// Entity tag showing that the entity is a content control.
        /// </summary>
        public struct InnerRightContent { };
        /// <summary>
        /// Entity tag showing that the entity is a content control.
        /// </summary>
        public struct InnerLeftContent { };
        /// <summary>
        /// Entity tag showing that the entity is a content control.
        /// </summary>
        public struct KeyBindings { };
        /// <summary>
        /// Entity tag showing that the 
        /// entity is a page. A page represents
        /// a root component in the UI hierarchy.
        /// Only one page entity can be a child of a parent.
        /// If you add a new page to a parent othe page of the parent is removed.
        /// An entity marked with a page tag wont get automatically destroyed when 
        /// its parent child relationship is removed.
        /// </summary>
        public struct Page
        {
        }
        /// <summary>
        /// Inidactes that the entity is a page that is being removed.
        /// </summary>
        public struct OldPage
        {
        }
        /// <summary>
        /// Inidactes that the entity is a page that is being added.
        /// </summary>
        public struct NewPage
        {
        }
        /// <summary>
        /// Entity tag showing that the entity is the current page.
        /// </summary>
        public record struct CurrentPage
        {
        }

        /// <summary>
        /// Initializes the module
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();
            RegisterEventDataComponents(world);

            world.Import<ECSObject>();

            world.Import<ECSInputElement>();
            world.Import<ECSInteractive>();
            world.Import<ECSLayoutable>();
            world.Import<ECSVisual>();
            world.Import<ECSContentControl>();
            world.Import<ECSItemsControl>();
            world.Import<ECSControl>();
            world.Import<ECSPopupFlyoutBase>();
            world.Import<ECSFlyoutBase>();
            world.Import<ECSMenuFlyout>();
            world.Import<ECSMenuItem>();
            world.Import<ECSDecorator>();
            world.Import<ECSMenu>();
            world.Import<ECSToolTip>();
            world.Import<ECSSelectingItemsControl>();
            world.Import<ECSToggleButton>();
            world.Import<ECSButton>();
            world.Import<ECSRepeatButton>();
            world.Import<ECSRadioButton>();
            world.Import<ECSSplitButton>();
            world.Import<ECSSeparator>();
            world.Import<ECSToggleSplitButton>();
            world.Import<ECSToggleSwitch>();
            world.Import<ECSBorder>();
            world.Import<ECSExpander>();
            world.Import<ECSHeaderedContentControl>();
            world.Import<ECSHeaderedSelectingItemsControl>();
            world.Import<ECSAutoCompleteBox>();
            world.Import<ECSCanvas>();
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
            world.Import<ECSCanvas>();
            world.Import<ECSImage>();
            world.Import<ECSShape>();
            world.Import<ECSRectangle>();
            world.Import<ECSViewbox>();

            AddUIComponentTags(world);
            AddControlToParentAdderObserver(world);
            AddFlyoutToControlObserver(world);
            RemoveControlFromParentObserver(world);
            RemoveControlComponentObserver(world);
            //AddPageObserver(world);


        }

        /// <summary>
        /// Adds the UI component tags to the world
        /// </summary>
        /// <param name="world"></param>
        public static void AddUIComponentTags(World world)
        {
            Logger.Debug("Adding UI component tags to world");
            world.Component<Page>("Page");
            world.Component<CurrentPage>("CurrentPage")
                .Add(Ecs.Exclusive)
                .Add(Ecs.Relationship);
            Logger.Debug("UI component tags added successfully");
        }

        /// <summary>
        /// This observer adds the flyout to the control
        /// when a child has the flyout component and 
        /// is attached to a parent control.
        /// </summary>
        /// <param name="world"></param>
        public static void AddFlyoutToControlObserver(World world)
        {
            Logger.Debug("Registering FlyoutToControl observer");
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
                            Logger.Debug("Adding flyout from child {ChildEntity} to parent control {ParentEntity}",
                                child.Name() ?? child.ToString(),
                                parent.Name() ?? parent.ToString());
                            parent.Get<Control>().ContextFlyout = child.Get<FlyoutBase>();
                        }
                    }
                });
            Logger.Debug("FlyoutToControl observer registered successfully");
        }

        /// <summary>
        /// Adds the control to parent adder observer.
        /// </summary>
        /// <param name="world"></param>
        public static void AddControlToParentAdderObserver(World world)
        {
            Logger.Debug("Registering ControlToParentAdder observer");
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
                                Logger.Debug("Control {ControlEntity} already exists in parent panel {PanelEntity}, skipping addition",
                                    child.Name() ?? child.ToString(),
                                    parent.Name() ?? parent.ToString());
                                return;
                            }
                            Logger.Debug("Adding control {ControlEntity} to parent panel {PanelEntity}",
                                child.Name() ?? child.ToString(),
                                parent.Name() ?? parent.ToString());
                            parent.Get<Panel>().Children.Add(control);
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            if (parent.Get<ContentControl>().Content == control)
                            {
                                Logger.Debug("Control {ControlEntity} already set as content for parent {ContentControlEntity}, skipping",
                                    child.Name() ?? child.ToString(),
                                    parent.Name() ?? parent.ToString());
                                return;
                            }
                            Logger.Debug("Setting control {ControlEntity} as content for parent {ContentControlEntity}",
                                child.Name() ?? child.ToString(),
                                parent.Name() ?? parent.ToString());
                            parent.Get<ContentControl>().Content = control;
                        }
                        else if (parent.Has<Viewbox>())
                        {
                            parent.Get<Viewbox>().Child = control;
                        }
                        else if (parent.Has<Border>())
                        {
                            parent.Get<Border>().Child = control;
                        }
                    }
                });
            Logger.Debug("ControlToParentAdder observer registered successfully");
        }

        /// <summary>
        /// Removes the control from parent when the Control component is removed from an entity.
        /// </summary>
        /// <param name="world"></param>
        public static void RemoveControlComponentObserver(World world)
        {
            Logger.Debug("Registering ControlComponentRemover observer");
            world.Observer<Control>("ControlComponentRemover")
                .Event(Ecs.OnRemove)
                .Each((Entity entity, ref Control control) =>
                {
                    // The control is already being removed, but we need to clean up parent references
                    if (entity.Has(Ecs.ChildOf, Ecs.Wildcard))
                    {
                        var parent = entity.Parent();

                        if (parent.Has<Panel>())
                        {
                            var panel = parent.Get<Panel>();
                            if (panel.Children.Contains(control))
                            {
                                Logger.Debug("Removing control from panel due to Control component removal on entity {EntityId}",
                                    entity.Name() ?? entity.ToString());
                                panel.Children.Remove(control);
                            }
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            var contentControl = parent.Get<ContentControl>();
                            if (contentControl.Content == control)
                            {
                                Logger.Debug("Clearing control from content control due to Control component removal on entity {EntityId}",
                                    entity.Name() ?? entity.ToString());
                                contentControl.Content = null;
                            }
                        }
                    }
                });
            Logger.Debug("ControlComponentRemover observer registered successfully");
        }

        /// <summary>
        /// Removes the control from parent when the child-parent relationship is removed.
        /// </summary>
        /// <param name="world"></param>
        public static void RemoveControlFromParentObserver(World world)
        {
            Logger.Debug("Registering ControlFromParentRemover observer");
            world.Observer("ControlFromParentRemover")
                .Event(Ecs.OnRemove)
                // Why without page?, Because sometimes we want to keep pages cached so we dont create them everytime, we switch to them
                // and instead have only one instance that is swapped with other pages.
                .Without<Page>()
                .With(Ecs.ChildOf, Ecs.Wildcard)
                .Each((child) =>
                {
                    // When the ChildOf relationship is removed, the second entity is the parent
                    var parent = child.Parent();

                    if (child.Has<Control>())
                    {
                        var control = child.Get<Control>();
                        if (parent.Has<Panel>())
                        {
                            var panel = parent.Get<Panel>();
                            if (panel.Children.Contains(control))
                            {
                                Logger.Debug("Removing control {ControlEntity} from parent panel {PanelEntity}",
                                    child.Name() ?? child.ToString(),
                                    parent.Name() ?? parent.ToString());
                                panel.Children.Remove(control);
                            }
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            var contentControl = parent.Get<ContentControl>();
                            if (contentControl.Content == control)
                            {
                                Logger.Debug("Clearing control {ControlEntity} from parent content control {ContentControlEntity}",
                                    child.Name() ?? child.ToString(),
                                    parent.Name() ?? parent.ToString());
                                contentControl.Content = null;
                            }
                        }
                    }
                });
            Logger.Debug("ControlFromParentRemover observer registered successfully");
        }

        /*
        Design NOTE:

        Maybe we want to refactor this. Here we have the idea of a page. A hierarchy of entities that represent something more 
        similar to a react component. The thing is that in react we have a single root component that is the page. 

        So a page is more of a root component. So maybe calling it differently would be better.

        We have a conceptual overlap and a mental model mismatch. 
        */
        /// <summary>
        /// Adds the page observer.
        /// </summary>
        /// <param name="world"></param>
        public static void AddPageObserver(World world)
        {
            Logger.Debug("Registering EnsureEntityHasOnlyOnePageChild observer");
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
                        Logger.Debug("Page {PageEntity} has no parent, skipping page check",
                            child.Name() ?? child.ToString());
                        return;
                    }

                    if (child.Has<Page>())
                    {
                        Logger.Debug("Found Page {PageEntity} attached to parent {ParentEntity}, checking for other pages",
                            child.Name() ?? child.ToString(),
                            parent.Name() ?? parent.ToString());

                        List<Entity> pages = [];
                        parent.Children((Entity child) =>
                        {
                            if (child.Has<Page>())
                            {
                                pages.Add(child);
                                Logger.Debug("Found additional page {PageEntity} under parent {ParentEntity}",
                                    child.Name() ?? child.ToString(),
                                    parent.Name() ?? parent.ToString());
                            }
                            if (pages.Count > 1)
                            {
                                Logger.Warn("Multiple pages found under parent {ParentEntity}, but unable to remove due to ordering issue",
                                    parent.Name() ?? parent.ToString());
                                // This does not work because we cannot count on the order
                                // of the entities in the list. Sometimes the first page added 
                                // is not the first child ecounterd with the page tag !
                                //pages[0].Remove(Ecs.ChildOf, Ecs.Wildcard);
                            }
                        });
                    }
                });
            Logger.Debug("EnsureEntityHasOnlyOnePageChild observer registered successfully");
        }
        /// <summary>
        /// Registers the event data components.
        /// </summary>
        /// <param name="world"></param>
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