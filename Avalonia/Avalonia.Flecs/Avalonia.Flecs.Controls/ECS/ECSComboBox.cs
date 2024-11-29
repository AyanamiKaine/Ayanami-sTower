using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSComboBox : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSComboBox>();
            world.Component<ComboBox>("ComboBox")
                            .OnSet((Entity e, ref ComboBox comboBox) =>
                            {
                                e.Set<ItemsControl>(comboBox);

                                comboBox.SelectionChanged += (object? sender, SelectionChangedEventArgs args) =>
                                {
                                    e.Set(new Events.SelectionChanged(sender, args));
                                    e.Emit<Events.SelectionChanged>();
                                };

                            }).OnRemove((Entity e, ref ComboBox comboBox) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = null;
                                }

                                if (parent.Has<Panel>())
                                {
                                    parent.Get<Panel>().Children.Remove(comboBox);
                                }
                            });
        }
    }
}
