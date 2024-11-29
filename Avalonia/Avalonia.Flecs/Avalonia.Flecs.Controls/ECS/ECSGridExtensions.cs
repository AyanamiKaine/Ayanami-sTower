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
            entity.Get<Grid>().RowDefinitions = rowDefinitions;
            return entity;
        }

        public static RowDefinitions GetRowDefinitions(this Entity entity)
        {
            return entity.Get<Grid>().RowDefinitions;
        }

        public static Entity SetColumnDefinitions(this Entity entity, ColumnDefinitions columnDefinitions)
        {
            entity.Get<Grid>().ColumnDefinitions = columnDefinitions;
            return entity;
        }

        public static ColumnDefinitions GetColumnDefinitions(this Entity entity)
        {
            return entity.Get<Grid>().ColumnDefinitions;
        }

    }
}