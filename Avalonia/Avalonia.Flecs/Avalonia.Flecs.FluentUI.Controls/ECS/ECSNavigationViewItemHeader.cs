using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public class ECSNavigationViewItemHeader : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSNavigationViewItemHeader>();
            world.Component<NavigationViewItemHeader>("NavigationViewItemHeader")
                .OnSet((Entity e, ref NavigationViewItemHeader navigationViewItemHeader) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<NavigationViewItem>())
                    {
                        parent.Get<NavigationViewItem>().MenuItems.Add(navigationViewItemHeader);
                    }
                }).OnRemove((Entity e, ref NavigationViewItemHeader navigationViewItemHeader) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<NavigationViewItem>())
                    {
                        parent.Get<NavigationViewItem>().MenuItems.Remove(navigationViewItemHeader);
                    }
                });
        }
    }
}