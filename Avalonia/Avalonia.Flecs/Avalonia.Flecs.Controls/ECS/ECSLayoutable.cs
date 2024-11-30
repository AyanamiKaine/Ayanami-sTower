using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSLayoutable : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSLayoutable>();
            world.Component<Layoutable>("Layoutable");
        }
    }
}
