using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
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
                               e.Set<object>(navigationView);
                               e.Set<ContentControl>(navigationView);

                           }).OnRemove((Entity e, ref NavigationView navigationView) =>
                           {
                               e.Remove<ContentControl>();
                           });
        }
    }
}
