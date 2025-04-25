using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the RepeatButton component
    /// </summary>
    public class ECSRepeatButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the RepeatButton component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSRepeatButton>();

            world
                .Component<RepeatButton>("RepeatButton")
                .OnSet(
                    (Entity e, ref RepeatButton repeatButton) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(repeatButton);
                        }
                        else if (e.Get<object>().GetType() == typeof(RepeatButton))
                        {
                            e.Set<object>(repeatButton);
                        }
                        // We set the contentControl component so systems and queries in general can more easily
                        // access the generic .content property of the button.
                        // This is good so queries can be more generic and not have to check for every possible control type.
                        e.Set<Button>(repeatButton);
                    }
                )
                .OnRemove((Entity e, ref RepeatButton repeatButton) => e.Remove<Button>());
        }
    }
}
