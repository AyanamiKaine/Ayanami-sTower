using Flecs.NET.Core;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Visual component
    /// </summary>
    public class ECSVisual : IFlecsModule
    {
        /// <summary>
        /// Initializes the Visual component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSVisual>();
            world.Component<Visual>("Visual")
                .OnSet((Entity e, ref Visual _) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(new Visual());
                    }
                });
        }
    }
}
