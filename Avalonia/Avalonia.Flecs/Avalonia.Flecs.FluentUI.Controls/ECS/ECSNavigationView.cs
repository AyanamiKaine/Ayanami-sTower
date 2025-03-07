using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Avalonia.Controls.Primitives;
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
                               if (!e.Has<object>())
                                   e.Set<object>(navigationView);
                               else if (e.Get<object>().GetType() == typeof(NavigationView))
                                   e.Set<object>(navigationView);

                               e.Set<HeaderedContentControl>(navigationView);
                               e.Set<ContentControl>(navigationView);
                           }).OnRemove((Entity e, ref NavigationView _) => e.Remove<ContentControl>());
        }
    }
}
