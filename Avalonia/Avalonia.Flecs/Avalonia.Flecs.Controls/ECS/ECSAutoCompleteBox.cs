using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the AutoCompleteBox component
    /// </summary>
    public class ECSAutoCompleteBox : IFlecsModule
    {
        /// <summary>
        /// Initializes the AutoCompleteBox component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSAutoCompleteBox>();
            world.Component<AutoCompleteBox>("AutoCompleteBox")
                .OnSet((Entity e, ref AutoCompleteBox autoCompleteBox) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(autoCompleteBox);
                    }

                    e.Set<TemplatedControl>(autoCompleteBox);
                })
                .OnRemove((Entity e, ref AutoCompleteBox _) => e.Remove<TemplatedControl>());
        }
    }
}
