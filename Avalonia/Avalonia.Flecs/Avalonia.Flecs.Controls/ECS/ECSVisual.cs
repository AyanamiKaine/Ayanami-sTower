using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Visual component
    /// </summary>
    public class ECSVisual : IFlecsModule
    {
        /// <summary>
        /// Initializes the Visual component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSVisual>();
            world.Component<Visual>("Visual");
        }
    }
}
