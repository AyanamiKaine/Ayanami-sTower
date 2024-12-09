using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to Window components
    /// </summary>
    public static class GetPropertyExtension
    {
        /// <summary>
        /// Helper function to get the property values of control elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static T GetProperty<T>(this Entity entity, string propertyName)
        {
            if (entity.Has<object>())
            {
                var obj = entity.Get<object>();
                var type = entity.Get<object>().GetType();
                var propertyInfo = type.GetProperty(propertyName);

                if (propertyInfo?.CanRead == true)
                {
                    try
                    {
                        var value = propertyInfo.GetValue(obj);
                        if (value != null)
                        {
                            return (T) Convert.ChangeType(value, typeof(T));
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Property {propertyName} not found from entity {entity.Id} in the object component", e);
                    }
                }
                throw new PropertyNotFoundException(entity, entity.Get<object>().GetType(), propertyName, nameof(GetProperty));
            }

            throw new ComponentNotFoundException(entity, entity.Get<object>().GetType(), nameof(GetProperty));
        }

        /// <summary>
        /// Helper function to set the property values of control elements
        /// using reflection.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="MissingFieldException"></exception>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetProperty(this Entity entity, string propertyName, object? value)
        {
            if (entity.Has<object>())
            {
                var obj = entity.Get<object>();
                var type = entity.Get<object>().GetType();
                var propertyInfo = type.GetProperty(propertyName);

                if (propertyInfo?.CanWrite == true)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);
                        propertyInfo.SetValue(obj, value);
                        return entity;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Property {propertyName} not found from entity {entity.Id} in the object component", e);
                    }
                }
                throw new PropertyNotFoundException(entity, entity.Get<object>().GetType(), propertyName, nameof(GetProperty));
            }
            throw new ComponentNotFoundException(entity, entity.Get<object>().GetType(), nameof(SetProperty));
        }
    }
}