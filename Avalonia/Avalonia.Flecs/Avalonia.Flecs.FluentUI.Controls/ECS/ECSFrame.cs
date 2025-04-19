using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Represents a Flecs module for integrating Avalonia Frame controls with the ECS world.
    /// </summary>
    public class ECSFrame : IFlecsModule
    {
        /// <summary>
        /// Initializes the ECSFrame module within the specified Flecs world.
        /// </summary>
        /// <param name="world">The Flecs world to initialize the module in.</param>
        public void InitModule(World world)
        {
            world.Module<ECSFrame>();

            world.Component<Frame>("Frame")
                .OnSet((Entity e, ref Frame frame) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(frame);
                    }
                    else if (e.Get<object>().GetType() == typeof(Frame))
                    {
                        e.Set<object>(frame);
                    }
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(frame);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = frame;
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
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });
        }
    }
}
