using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to getting and running methods
    /// on specific avalonia controls where we dont know the exact type
    /// but know an specific method exists.
    /// </summary>
    public static class ECSMethodExtension
    {

        /// <summary>
        /// Tries running a method on a control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="MissingMethodException"></exception>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity RunMethod(this Entity entity, string methodName, params object[] args)
        {
            if (entity.Has<object>())
            {
                var control = entity.Get<object>();
                // I personally dont really know how reflection works and
                // what happens using GetMethod
                var method = control.GetType().GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    args?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes,
                    null);

                if (method != null)
                {
                    method.Invoke(control, args);
                }
                else
                {
                    throw new MissingMethodException($"Method {methodName} not found on {control.GetType().Name}");
                }

                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(object), nameof(RunMethod));
        }

        /// <summary>
        /// Gets a method from a control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static MethodInfo GetMethod(this Entity entity, string methodName)
        {
            if (entity.Has<object>())
            {
                var control = entity.Get<object>();

                var methodInfo = control.GetType().GetMethod(methodName);
                if (methodInfo != null)
                    return methodInfo;
            }
            throw new ComponentNotFoundException(entity, typeof(object), nameof(GetMethod));
        }

        /// <summary>
        /// Gets a method from a control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="methodName"></param>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        /// <exception cref="MissingMethodException"></exception>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static MethodInfo GetMethod(this Entity entity, string methodName, params Type[] parameterTypes)
        {
            if (entity.Has<object>())
            {
                var control = entity.Get<object>();
                var methodInfo = control.GetType().GetMethod(methodName, parameterTypes);
                if (methodInfo != null)
                    return methodInfo;
                throw new MissingMethodException($"Method {methodName} not found on {control.GetType().Name}");
            }
            throw new ComponentNotFoundException(entity, typeof(object), nameof(GetMethod));
        }
    }
}