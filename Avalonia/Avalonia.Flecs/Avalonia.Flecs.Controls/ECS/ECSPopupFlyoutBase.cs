using Flecs.NET.Core;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the PopupFlyoutBase component
    /// </summary>
    public class ECSPopupFlyoutBase : IFlecsModule
    {
        /// <summary>
        /// Initializes the PopupFlyoutBase component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSPopupFlyoutBase>();
            world.Component<PopupFlyoutBase>("MenuFlyout")
                .OnSet((Entity e, ref PopupFlyoutBase popupFlyoutBase) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(popupFlyoutBase);
                    }
                    else if (e.Get<object>().GetType() == typeof(PopupFlyoutBase))
                    {
                        e.Set<object>(popupFlyoutBase);
                    }
                    e.Set<FlyoutBase>(popupFlyoutBase);
                })
                .OnRemove((Entity e, ref PopupFlyoutBase popupFlyoutBase) => e.Remove<FlyoutBase>());
        }
    }
}