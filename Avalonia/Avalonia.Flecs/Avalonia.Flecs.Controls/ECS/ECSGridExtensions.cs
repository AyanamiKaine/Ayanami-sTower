using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    public static class ECSGridExtensions
    {
        /// <summary>
        /// Helper function to set the row definitions of a Grid control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="rowDefinitions"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetRowDefinitions(this Entity entity, RowDefinitions rowDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().RowDefinitions = rowDefinitions;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(SetRowDefinitions));

        }
        /// <summary>
        /// Helper function to get the row definitions of a Grid control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static RowDefinitions GetRowDefinitions(this Entity entity)
        {
            if (entity.Has<Grid>())
            {
                return entity.Get<Grid>().RowDefinitions;
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(GetRowDefinitions));

        }

        /// <summary>
        /// Helper function to set the column definitions of a Grid control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="columnDefinitions"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetColumnDefinitions(this Entity entity, ColumnDefinitions columnDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().ColumnDefinitions = columnDefinitions;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(SetColumnDefinitions));

        }

        /// <summary>
        /// Helper function to get the column definitions of a Grid control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static ColumnDefinitions GetColumnDefinitions(this Entity entity)
        {
            if (entity.Has<Grid>())
            {
                return entity.Get<Grid>().ColumnDefinitions;
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(GetColumnDefinitions));
        }
    }
}