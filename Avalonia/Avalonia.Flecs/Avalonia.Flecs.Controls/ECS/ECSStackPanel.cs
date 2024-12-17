using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the StackPanel component
    /// </summary>
    public class ECSStackPanel : IFlecsModule
    {
        /// <summary>
        /// Initializes the StackPanel component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSStackPanel>();
            world.Component<StackPanel>("StackPanel")
                .OnSet((Entity e, ref StackPanel stackPanel) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(stackPanel);
                    }
                    else if (e.Get<object>().GetType() == typeof(StackPanel))
                    {
                        e.Set<object>(stackPanel);
                    }
                    e.Set<Panel>(stackPanel);
                }).OnRemove((Entity e, ref StackPanel stackPanel) => e.Remove<Panel>());
        }
    }
}
