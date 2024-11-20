using Avalonia.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {

        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();

            world.Component<NavigationView>("NavigationView")
                .OnSet((Entity e, ref NavigationView navigationView) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = navigationView;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(navigationView);
                    }
                }).OnRemove((Entity e, ref NavigationView navigationView) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(navigationView);
                    }
                });

            world.Component<NavigationViewItem>("NavigationViewItem")
                .OnSet((Entity e, ref NavigationViewItem navigationViewItem) =>
                {
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

            world.Component<Frame>("Frame")
                .OnSet((Entity e, ref Frame frame) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(frame);
                    }
                }).OnRemove((Entity e, ref Frame frame) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(frame);
                    }
                });
        }
    }
}