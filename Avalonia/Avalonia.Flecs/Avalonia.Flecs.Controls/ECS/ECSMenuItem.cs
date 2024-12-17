using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the MenuItem component
    /// </summary>
    public class ECSMenuItem : IFlecsModule
    {
        /// <summary>
        /// Initializes the MenuItem component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSMenuItem>();
            world.Component<MenuItem>("MenuItem")
                            .OnSet((Entity e, ref MenuItem menuItem) =>
                            {
                                if (!e.Has<object>())
                                {
                                    e.Set<object>(menuItem);
                                }
                                else if (e.Get<object>() is MenuItem)
                                {
                                    e.Set<object>(menuItem);
                                }
                                else
                                {
                                    e.Set<object>(menuItem);
                                }
                                e.Set<HeaderedSelectingItemsControl>(menuItem);

                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<Menu>())
                                {
                                    parent.Get<Menu>().Items.Add(menuItem);
                                }
                                else if (parent.Has<MenuItem>())
                                {
                                    parent.Get<MenuItem>().Items.Add(menuItem);
                                }
                                else if (parent.Has<MenuFlyout>())
                                {
                                    parent.Get<MenuFlyout>().Items.Add(menuItem);
                                }
                            }).OnRemove((Entity e, ref MenuItem menuItem) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<Menu>())
                                {
                                    parent.Get<Menu>().Items.Remove(menuItem);
                                }
                                else if (parent.Has<MenuItem>())
                                {
                                    parent.Get<MenuItem>().Items.Remove(menuItem);
                                }
                            });
        }
    }
}
