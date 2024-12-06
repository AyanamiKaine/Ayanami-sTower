using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ComboBox component
    /// </summary>
    public class ECSComboBox : IFlecsModule
    {
        /// <summary>
        /// Initializes the ComboBox component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSComboBox>();
            world.Component<ComboBox>("ComboBox")
                            .OnSet((Entity e, ref ComboBox comboBox) =>
                            {
                                if (!e.Has<object>())
                                {
                                    e.Set<object>(comboBox);
                                }
                                e.Set<ItemsControl>(comboBox);

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
