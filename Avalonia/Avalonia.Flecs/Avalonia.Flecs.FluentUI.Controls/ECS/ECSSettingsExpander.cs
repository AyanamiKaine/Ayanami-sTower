using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public class ECSSettingsExpander : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSSettingsExpander>();
            world.Component<SettingsExpander>("SettingsExpander")
                            .OnSet((Entity e, ref SettingsExpander settingsExpander) =>
                            {
                                if (!e.Has<object>())
                                    e.Set<object>(settingsExpander);
                                else if (e.Get<object>().GetType() == typeof(SettingsExpander))
                                    e.Set<object>(settingsExpander);

                                e.Set<HeaderedItemsControl>(settingsExpander);
                                e.Set<Control>(settingsExpander);
                            }).OnRemove((Entity e, ref SettingsExpander _) => e.Remove<HeaderedItemsControl>());
        }
    }
}
