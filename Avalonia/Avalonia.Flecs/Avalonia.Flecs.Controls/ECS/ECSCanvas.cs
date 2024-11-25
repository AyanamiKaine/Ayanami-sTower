using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSCanvas : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSCanvas>();
            world.Component<Canvas>("Canvas")
                .OnSet((Entity e, ref Canvas canvas) =>
                {
                    e.Set<Panel>(canvas);

                }).OnRemove((Entity e, ref Canvas canvas) =>
                {

                    e.Remove<Panel>();
                });
        }
    }
}
