using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    public static class ECSGridExtensions
    {
        public static Entity SetRowDefinitions(this Entity entity, RowDefinitions rowDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().RowDefinitions = rowDefinitions;
                return entity;
            }
            throw new Exception("Entity does not have a Grid component. Try adding a control element that is an Grid control to the entity.");
        }

        public static RowDefinitions GetRowDefinitions(this Entity entity)
        {
            if (entity.Has<Grid>())
            {
                return entity.Get<Grid>().RowDefinitions;
            }
            throw new Exception("Entity does not have a Grid component. Try adding a control element that is an Grid control to the entity.");
        }

        public static Entity SetColumnDefinitions(this Entity entity, ColumnDefinitions columnDefinitions)
        {
            if (entity.Has<Grid>())
            {
                entity.Get<Grid>().ColumnDefinitions = columnDefinitions;
                return entity;
            }
            throw new Exception("Entity does not have a Grid component. Try adding a control element that is an Grid control to the entity.");
        }

        public static ColumnDefinitions GetColumnDefinitions(this Entity entity)
        {
            if (entity.Has<Grid>())
            {
                return entity.Get<Grid>().ColumnDefinitions;
            }
            throw new Exception("Entity does not have a Grid component. Try adding a control element that is an Grid control to the entity.");
        }

    }
}