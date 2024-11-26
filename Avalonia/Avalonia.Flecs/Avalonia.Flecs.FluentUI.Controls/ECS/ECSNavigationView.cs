using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.FluentUI.Controls.ECS.Module;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public class ECSNavigationView : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSNavigationView>();
            world.Component<NavigationView>("NavigationView")
                           .OnSet((Entity e, ref NavigationView navigationView) =>
                           {
                               e.Set<ContentControl>(navigationView);

                               navigationView.SelectionChanged += (object sender, NavigationViewSelectionChangedEventArgs args) =>
                               {
                                   e.Set(new Events.OnSelectionChanged(sender, args));
                                   e.Emit<Events.OnSelectionChanged>();
                               };


                           }).OnRemove((Entity e, ref NavigationView navigationView) =>
                           {
                               e.Remove<ContentControl>();
                           });
        }
    }
}
