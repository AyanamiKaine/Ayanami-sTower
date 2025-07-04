using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ToolTip component
    /// </summary>
    public class ECSToolTip : IFlecsModule
    {
        /// <summary>
        /// Initializes a ToolTip component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSToolTip>();
            world
                .Component<ToolTip>("ToolTip")
                .OnSet(
                    (Entity e, ref ToolTip toolTip) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(toolTip);
                        }
                        else if (e.Get<object>().GetType() == typeof(ToolTip))
                        {
                            e.Set<object>(toolTip);
                        }
                        e.Set<ContentControl>(toolTip);
                    }
                )
                .OnRemove((Entity e, ref ToolTip _) => e.Remove<object>());
        }
    }
}
