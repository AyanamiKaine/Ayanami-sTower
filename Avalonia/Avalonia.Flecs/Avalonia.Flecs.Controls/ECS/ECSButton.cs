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
    public class ECSButton : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSButton>();

            world.Component<Button>("Button")
                .OnSet((Entity e, ref Button button) =>
                {

                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<ContentControl>(button);
                    
                    /// IMPORTANT
                    /// ALL OBERSERVES RUN IN A NON-UI THREAD THIS IS THE DEFAULT BEHAVIOR IN AVALONIA
                    /// ANY CODE EXECUTED IN AN OBSERVE THAT MODIFIES THE UI MUST BE DISPATCHED TO THE UI THREAD
                    /// THIS CAN BE DONE BY USING THE 
                    /// Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { /* UI CODE HERE */ });
                    /// THIS ALSO MATTER FOR ALL FUNCTIONS 
                    /// THAT WANT TO USE THE ECS WORLD FOUND IN MAIN THE APPLICATION
                    button.Click += (object? sender, RoutedEventArgs args) =>
                    {
                        e.Set(new Click(sender, args));
                        e.Emit<Click>();
                    };

                    button.TemplateApplied += (object? sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Set(new TemplateApplied(sender, args));
                        e.Emit<TemplateApplied>();
                    };

                })
                .OnRemove((Entity e, ref Button button) =>
                {
                    e.Remove<ContentControl>();
                });
        }
    }
}
