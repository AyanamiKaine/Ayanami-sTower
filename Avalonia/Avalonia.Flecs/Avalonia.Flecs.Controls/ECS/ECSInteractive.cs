using Flecs.NET.Core;
using Avalonia.Interactivity;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Interactive component
    /// </summary>
    public class ECSInteractive : IFlecsModule
    {
        /// <summary>
        /// Initializes the Interactive component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSInteractive>();
            world.Component<Interactive>("Interactive")
                .OnSet((Entity e, ref Interactive interactive) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(interactive);
                    }
                    else if (e.Get<object>().GetType() == typeof(Interactive))
                    {
                        e.Set<object>(interactive);
                    }
                });
        }
    }
}
