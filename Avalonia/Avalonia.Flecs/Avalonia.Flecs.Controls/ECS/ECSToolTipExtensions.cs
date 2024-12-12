using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    /// <summary>
    /// Various helper functions working with ToolTips
    /// </summary>
    public static class ECSToolTipExtensions
    {
        /// <summary>
        /// Adds a tooltip entity to a entity with a contentControl component
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="toolTipEntity">We attach the tooltip to this entities avalonia component</param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity AttachToolTip(this Entity entity, Entity toolTipEntity)
        {
            if (toolTipEntity.Has<ToolTip>() && entity.Has<Control>())
            {
                ToolTip.SetTip(entity.Get<Control>(), toolTipEntity.Get<ToolTip>());
                return entity;
            }

            if (!toolTipEntity.Has<ToolTip>())
                throw new ComponentNotFoundException(toolTipEntity, typeof(ToolTip), nameof(AttachToolTip));
            else
                throw new ComponentNotFoundException(entity, typeof(Control), nameof(AttachToolTip));
        }

        /// <summary>
        /// Helper function to get the tooltip that is attached to an entities underlying
        /// control class/component
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static object? GetAttachedToolTip(this Entity entity)
        {
            if (entity.Has<Control>())
            {
                return ToolTip.GetTip(entity.Get<Control>());
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(GetAttachedToolTip));
        }
    }
}