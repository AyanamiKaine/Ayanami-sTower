using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls.ECS.Events;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSTemplatedControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSTemplatedControl>();
            world.Component<TemplatedControl>("TemplatedControl")
                .OnSet((Entity e, ref TemplatedControl templatedControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(templatedControl);
                    }
                    e.Set<Control>(templatedControl);

                    templatedControl.TemplateApplied += (object? sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Set(new TemplateApplied(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                })
                .OnRemove((Entity e, ref TemplatedControl templatedControl) =>
                {
                    e.Remove<Control>();
                });
        }
    }
}
