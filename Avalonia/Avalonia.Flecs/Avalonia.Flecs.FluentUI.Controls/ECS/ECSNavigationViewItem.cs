using Avalonia.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public class ECSNavigationViewItem : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSNavigationViewItem>();
            world.Component<NavigationViewItem>("NavigationViewItem")
                            .OnSet((Entity e, ref NavigationViewItem navigationViewItem) =>
                            {
                                if (!e.Has<object>())
                                    e.Set<object>(navigationViewItem);
                                else if (e.Get<object>().GetType() == typeof(NavigationViewItem))
                                    e.Set<object>(navigationViewItem);

                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<NavigationView>())
                                {
                                    parent.Get<NavigationView>().MenuItems.Add(navigationViewItem);
                                }
                            }).OnRemove((Entity e, ref NavigationViewItem navigationViewItem) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<NavigationView>())
                                {
                                    parent.Get<NavigationView>().MenuItems.Remove(navigationViewItem);
                                }
                            });
        }
    }
}
