using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the TemplatedControl component
    /// </summary>
    public class ECSTemplatedControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the TemplatedControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSTemplatedControl>();
            world.Component<TemplatedControl>("TemplatedControl")
                .OnSet((Entity e, ref TemplatedControl templatedControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(templatedControl);
                    }
                    e.Set<Control>(templatedControl);
                })
                .OnRemove((Entity e, ref TemplatedControl templatedControl) =>
                {
                    e.Remove<Control>();
                });
        }
    }
}
