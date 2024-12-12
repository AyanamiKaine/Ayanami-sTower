using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// Extensions methods related to flyouts
    /// </summary>
    public static class ECSFlyoutExtensions
    {
        /// <summary>
        /// Sets the context flyout of a control component
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetContextFlyout(this Entity entity, FlyoutBase content)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().ContextFlyout = content;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ContextFlyout", content);
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(SetContextFlyout));
        }

        /// <summary>
        /// Sets the context flyout of a control component
        /// </summary>
        /// <param name="entity">Entity that gets an flyout attached</param>
        /// <param name="contentEntity">Entity that has a flyoutbase attached to it</param>
        /// <returns></returns>
        public static Entity SetContextFlyout(this Entity entity, Entity contentEntity)
        {
            if (contentEntity.Has<FlyoutBase>())
                return entity.SetContextFlyout(contentEntity.Get<FlyoutBase>());
            throw new ComponentNotFoundException(contentEntity, typeof(FlyoutBase), nameof(SetContextFlyout));
        }
    }
}