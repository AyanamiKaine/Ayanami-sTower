using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Grid component
    /// </summary>
    public class ECSGrid : IFlecsModule
    {
        /// <summary>
        /// Initializes the Grid component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSGrid>();
            world
                .Component<Grid>("Grid")
                .OnSet(
                    (Entity e, ref Grid grid) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(grid);
                        }
                        else if (e.Get<object>().GetType() == typeof(Grid))
                        {
                            e.Set<object>(grid);
                        }
                        // We set the panel component so systems and queries in general can more easily
                        // access the generic .children.add property of the panel.
                        // This is good so queries can be more generic and not have to check for every possible panel type.
                        e.Set<Panel>(grid);
                    }
                )
                .OnRemove((Entity e, ref Grid grid) => e.Remove<Panel>());
        }
    }
}
