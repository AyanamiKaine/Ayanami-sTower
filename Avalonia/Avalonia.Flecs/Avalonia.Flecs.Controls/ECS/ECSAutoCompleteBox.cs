using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSAutoCompleteBox : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSAutoCompleteBox>();
            world.Component<AutoCompleteBox>("AutoCompleteBox")
                .OnSet((Entity e, ref AutoCompleteBox autoCompleteBox) =>
                {
                    e.Set<TemplatedControl>(autoCompleteBox);
                })
                .OnRemove((Entity e, ref AutoCompleteBox autoCompleteBox) =>
                {
                    e.Remove<TemplatedControl>();
                });
        }
    }
}
