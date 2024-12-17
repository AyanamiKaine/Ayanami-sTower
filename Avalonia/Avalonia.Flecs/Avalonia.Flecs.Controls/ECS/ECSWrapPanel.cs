using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the WrapPanel component
    /// </summary>
    public class ECSWrapPanel : IFlecsModule
    {
        /// <summary>
        /// Initializes the WrapPanel component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSWrapPanel>();
            world.Component<WrapPanel>("WrapPanel")
                .OnSet((Entity e, ref WrapPanel wrapPanel) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(wrapPanel);
                    }
                    else if (e.Get<object>().GetType() == typeof(WrapPanel))
                    {
                        e.Set<object>(wrapPanel);
                    }
                    e.Set<Panel>(wrapPanel);
                }).OnRemove((Entity e, ref WrapPanel _) => e.Remove<Panel>());
        }
    }
}
