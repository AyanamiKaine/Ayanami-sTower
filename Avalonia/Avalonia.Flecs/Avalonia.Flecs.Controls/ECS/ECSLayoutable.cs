using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Layoutable component
    /// </summary>
    public class ECSLayoutable : IFlecsModule
    {
        /// <summary>
        /// Initializes the Layoutable component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSLayoutable>();
            world.Component<Layoutable>("Layoutable");
        }
    }
}
