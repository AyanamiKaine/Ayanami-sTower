using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSInteractive : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSInteractive>();
            world.Component<Interactive>("Interactive")
                .OnSet((Entity e, ref Interactive interactive) =>
                {

                }).OnRemove((Entity e, ref Interactive interactive) =>
                {
                });
        }
    }
}
