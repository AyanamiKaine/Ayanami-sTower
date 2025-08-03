using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using AyanamisTower.StellaEcs.Attributes;

namespace AyanamisTower.StellaEcs.Extensions;

/// <summary>
/// Runtime validation and helper methods for entity extension methods.
/// In a full implementation, this would be mostly replaced by source generation.
/// </summary>
public static class EntityExtensionValidator
{
    private static readonly Dictionary<MethodInfo, Type[]> _methodRequirements = new();
    private static readonly Dictionary<MethodInfo, bool> _allowPartial = new();

    /// <summary>
    /// Validates that an entity has all required components for a method call.
    /// This would typically be inlined by source generation.
    /// </summary>
    public static void ValidateRequirements(Entity entity, [CallerMemberName] string methodName = "")
    {
        var method = GetCallingMethod(methodName);
        if (method == null) return;

        var requirements = GetMethodRequirements(method);
        var allowPartial = GetAllowPartial(method);

        foreach (var componentType in requirements)
        {
            if (!entity.Has(componentType))
            {
                if (allowPartial) continue;

                throw new InvalidOperationException(
                    $"Entity {entity} does not have required component {componentType.Name} " +
                    $"for method {methodName}");
            }
        }
    }

    /// <summary>
    /// Checks if an entity satisfies all requirements for a method without throwing.
    /// </summary>
    public static bool CanExecute(Entity entity, [CallerMemberName] string methodName = "")
    {
        var method = GetCallingMethod(methodName);
        if (method == null) return false;

        var requirements = GetMethodRequirements(method);

        foreach (var componentType in requirements)
        {
            if (!entity.Has(componentType))
                return false;
        }

        return true;
    }

    private static MethodInfo? GetCallingMethod(string methodName)
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var frames = stackTrace.GetFrames();

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method?.Name == methodName && method.IsStatic)
            {
                return (MethodInfo?)method;
            }
        }

        return null;
    }

    private static Type[] GetMethodRequirements(MethodInfo method)
    {
        if (_methodRequirements.TryGetValue(method, out var cached))
            return cached;

        var attr = method.GetCustomAttribute<EntityExtensionAttribute>();
        if (attr?.RequiredComponents != null)
        {
            _methodRequirements[method] = attr.RequiredComponents;
            return attr.RequiredComponents;
        }

        // Infer from parameters marked with ComponentAttribute
        var requirements = new List<Type>();
        var parameters = method.GetParameters();

        for (int i = 1; i < parameters.Length; i++) // Skip first parameter (Entity)
        {
            var param = parameters[i];
            var componentAttr = param.GetCustomAttribute<ComponentAttribute>();

            if (componentAttr != null && param.ParameterType.IsValueType)
            {
                requirements.Add(param.ParameterType);
            }
        }

        var result = requirements.ToArray();
        _methodRequirements[method] = result;
        return result;
    }

    private static bool GetAllowPartial(MethodInfo method)
    {
        if (_allowPartial.TryGetValue(method, out var cached))
            return cached;

        var attr = method.GetCustomAttribute<EntityExtensionAttribute>();
        var result = attr?.AllowPartial ?? false;
        _allowPartial[method] = result;
        return result;
    }
}

/// <summary>
/// Advanced entity extension methods that provide more sophisticated component access patterns.
/// </summary>
public static class AdvancedEntityExtensions
{
    /// <summary>
    /// Executes an action conditionally based on component availability.
    /// Uses a fluent API for complex conditional logic.
    /// </summary>
    public static EntityConditionalBuilder If<T>(this Entity entity) where T : struct
    {
        return new EntityConditionalBuilder(entity, entity.Has<T>());
    }

    /// <summary>
    /// Executes different actions based on which components are present.
    /// </summary>
    public static void Switch<T1, T2>(this Entity entity,
        Action<T1>? onT1 = null,
        Action<T2>? onT2 = null,
        Action<T1, T2>? onBoth = null,
        Action? onNeither = null)
        where T1 : struct where T2 : struct
    {
        bool hasT1 = entity.Has<T1>();
        bool hasT2 = entity.Has<T2>();

        if (hasT1 && hasT2 && onBoth != null)
        {
            onBoth(entity.Get<T1>(), entity.Get<T2>());
        }
        else if (hasT1 && onT1 != null)
        {
            onT1(entity.Get<T1>());
        }
        else if (hasT2 && onT2 != null)
        {
            onT2(entity.Get<T2>());
        }
        else if (!hasT1 && !hasT2 && onNeither != null)
        {
            onNeither();
        }
    }

    /// <summary>
    /// Applies a transformation to a component if it exists.
    /// </summary>
    public static bool Transform<T>(this Entity entity, Func<T, T> transformer) where T : struct
    {
        if (!entity.Has<T>()) return false;

        var component = entity.Get<T>();
        var transformed = transformer(component);
        entity.Set(transformed);
        return true;
    }

    /// <summary>
    /// Gets a component with a default fallback if it doesn't exist.
    /// </summary>
    public static T GetOrDefault<T>(this Entity entity, T defaultValue = default) where T : struct
    {
        return entity.Has<T>() ? entity.Get<T>() : defaultValue;
    }

    /// <summary>
    /// Ensures a component exists, adding it with a default value if necessary.
    /// Returns a mutable reference to the component.
    /// </summary>
    public static ref T EnsureComponent<T>(this Entity entity, T defaultValue = default) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add(defaultValue);
        }
        return ref entity.GetMutable<T>();
    }
}

/// <summary>
/// Builder for conditional entity operations.
/// </summary>
public struct EntityConditionalBuilder
{
    private readonly Entity _entity;
    private readonly bool _condition;

    internal EntityConditionalBuilder(Entity entity, bool condition)
    {
        _entity = entity;
        _condition = condition;
    }

    /// <summary>
    /// Executes the action if the condition is true.
    /// </summary>
    public readonly EntityConditionalBuilder Then<T>(Action<T> action) where T : struct
    {
        if (_condition && _entity.Has<T>())
        {
            action(_entity.Get<T>());
        }
        return this;
    }

    /// <summary>
    /// Adds an additional condition.
    /// </summary>
    public readonly EntityConditionalBuilder And<T>() where T : struct
    {
        return new EntityConditionalBuilder(_entity, _condition && _entity.Has<T>());
    }

    /// <summary>
    /// Executes the action if none of the conditions were met.
    /// </summary>
    public readonly void Else(Action action)
    {
        if (!_condition)
        {
            action();
        }
    }
}
