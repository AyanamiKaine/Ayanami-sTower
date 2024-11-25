using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSRelativePanel : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSRelativePanel>();
            world.Component<RelativePanel>("RelativePanel")
                .OnSet((Entity e, ref RelativePanel relativePanel) =>
                {
                    e.Set<Panel>(relativePanel);

                }).OnRemove((Entity e, ref RelativePanel relativePanel) =>
                {

                    e.Remove<Panel>();
                });
        }
    }
}
