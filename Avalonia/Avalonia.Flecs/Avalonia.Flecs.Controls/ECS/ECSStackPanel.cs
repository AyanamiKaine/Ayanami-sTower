using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSStackPanel : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSStackPanel>();
            world.Component<StackPanel>("StackPanel")
                .OnSet((Entity e, ref StackPanel stackPanel) =>
                {
                    e.Set<Panel>(stackPanel);

                }).OnRemove((Entity e, ref StackPanel stackPanel) =>
                {

                    e.Remove<Panel>();
                });
        }
    }
}
