using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
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