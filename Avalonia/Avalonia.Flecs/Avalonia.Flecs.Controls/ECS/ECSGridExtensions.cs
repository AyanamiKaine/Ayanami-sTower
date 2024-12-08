using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to Grid components
    /// </summary>
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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("RowDefinitions", rowDefinitions);
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(SetRowDefinitions));

        }

        /// <summary>
        /// Given a string creates a row definition and sets it for the entity
        /// if it has a Grid Component Attached
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="rowDefinitions"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetRowDefinitions(this Entity entity, string rowDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().RowDefinitions = new RowDefinitions(rowDefinitions);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("RowDefinitions", new RowDefinitions(rowDefinitions));
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
            else if (entity.Has<object>())
            {
                return entity.GetProperty<RowDefinitions>("RowDefinitions");
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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ColumnDefinitions", columnDefinitions);
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(SetColumnDefinitions));

        }

        /// <summary>
        /// Given a string creates a column definition and sets it for the
        /// entity if it has a grid component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="columnDefinitions"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetColumnDefinitions(this Entity entity, string columnDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().ColumnDefinitions = new ColumnDefinitions(columnDefinitions);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ColumnDefinitions", new ColumnDefinitions(columnDefinitions));
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
            else if (entity.Has<object>())
            {
                return entity.GetProperty<ColumnDefinitions>("ColumnDefinitions");
            }
            throw new ComponentNotFoundException(entity, typeof(Grid), nameof(GetColumnDefinitions));
        }
    }
}