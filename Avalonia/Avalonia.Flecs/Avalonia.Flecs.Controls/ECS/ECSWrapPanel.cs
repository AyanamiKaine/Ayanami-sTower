using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSWrapPanel : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSWrapPanel>();
            world.Component<WrapPanel>("WrapPanel")
                .OnSet((Entity e, ref WrapPanel wrapPanel) =>
                {

                    e.Set<Panel>(wrapPanel);

                }).OnRemove((Entity e, ref WrapPanel wrapPanel) =>
                {

                    e.Remove<Panel>();
                });
        }
    }
}
