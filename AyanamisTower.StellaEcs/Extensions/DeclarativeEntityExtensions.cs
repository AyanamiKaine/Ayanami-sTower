using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AyanamisTower.StellaEcs.Attributes;

namespace AyanamisTower.StellaEcs.Extensions;

/// <summary>
/// Provides automatic component requirement checking and method execution
/// based on declarative attributes. This eliminates the need for manual HasAll checks.
/// </summary>
public static class DeclarativeEntityExtensions
{
    /// <summary>
    /// Executes an action on an entity only if it has all the required components
    /// specified by With attributes. This is the core method that enables the declarative syntax.
    /// </summary>
    /// <param name="entity">The entity to operate on</param>
    /// <param name="action">The action to execute</param>
    /// <param name="methodName">The calling method name (automatically provided)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteIfComponents(this Entity entity, Action action, [CallerMemberName] string methodName = "")
    {
        if (HasRequiredComponents(entity, methodName))
        {
            action();
        }
    }

    /// <summary>
    /// Executes an action on an entity only if it has all required components,
    /// returning whether the action was executed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryExecuteIfComponents(this Entity entity, Action action, [CallerMemberName] string methodName = "")
    {
        if (HasRequiredComponents(entity, methodName))
        {
            action();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Executes a function and returns its result only if the entity has all required components.
    /// Returns the default value if components are missing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ExecuteIfComponents<T>(this Entity entity, Func<T> func, T? defaultValue = default, [CallerMemberName] string methodName = "")
    {
        if (HasRequiredComponents(entity, methodName))
        {
            return func();
        }
        return defaultValue;
    }

    /// <summary>
    /// Checks if an entity has all components required by a method's With attributes.
    /// </summary>
    private static bool HasRequiredComponents(Entity entity, string methodName)
    {
        var requirements = GetMethodRequirements(methodName);
        return requirements.All(componentType => entity.Has(componentType));
    }

    private static readonly Dictionary<string, Type[]> _methodRequirementsCache = new();

    /// <summary>
    /// Gets the component requirements for a method by examining its With attributes.
    /// Results are cached for performance.
    /// </summary>
    private static Type[] GetMethodRequirements(string methodName)
    {
        if (_methodRequirementsCache.TryGetValue(methodName, out var cached))
            return cached;

        // In a real implementation, this would use source generation
        // For now, we'll use reflection to find the method
        var stackTrace = new System.Diagnostics.StackTrace();
        var frames = stackTrace.GetFrames();

        MethodInfo? targetMethod = null;
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method?.Name == methodName && method.IsStatic)
            {
                targetMethod = (MethodInfo?)method;
                break;
            }
        }

        if (targetMethod == null)
        {
            _methodRequirementsCache[methodName] = Array.Empty<Type>();
            return Array.Empty<Type>();
        }

        var withAttributes = targetMethod.GetCustomAttributes<WithAttribute>();
        var requirements = withAttributes.Select(attr => attr.ComponentType).ToArray();

        _methodRequirementsCache[methodName] = requirements;
        return requirements;
    }
}

/// <summary>
/// Macro-style methods that provide even more ergonomic syntax for common patterns.
/// These can be used in extension methods to eliminate boilerplate.
/// </summary>
public static class ComponentMacros
{
    /// <summary>
    /// Executes an action with automatic component access for a single component.
    /// The component type is inferred from the action parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithComponent<T>(this Entity entity, Action<T> action) where T : struct
    {
        if (entity.Has<T>())
        {
            action(entity.Get<T>());
        }
    }

    /// <summary>
    /// Executes an action with automatic mutable component access.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithComponentMutable<T>(this Entity entity, RefAction<T> action) where T : struct
    {
        if (entity.Has<T>())
        {
            action(ref entity.GetMutable<T>());
        }
    }

    /// <summary>
    /// Executes an action with automatic component access for two components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithComponents<T1, T2>(this Entity entity, Action<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (entity.Has<T1>() && entity.Has<T2>())
        {
            action(entity.Get<T1>(), entity.Get<T2>());
        }
    }

    /// <summary>
    /// Executes an action with mixed mutable/readonly access to two components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithComponentsMutable<T1, T2>(this Entity entity, RefAction<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (entity.Has<T1>() && entity.Has<T2>())
        {
            action(ref entity.GetMutable<T1>(), ref entity.GetMutable<T2>());
        }
    }

    /// <summary>
    /// Executes an action with the first component mutable and second readonly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithComponentsMixed<T1, T2>(this Entity entity, MixedRefAction<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (entity.Has<T1>() && entity.Has<T2>())
        {
            action(ref entity.GetMutable<T1>(), entity.Get<T2>());
        }
    }
}

/// <summary>
/// Delegate for actions that take one mutable and one readonly component parameter.
/// </summary>
public delegate void MixedRefAction<T1, T2>(ref T1 mutableComponent, in T2 readonlyComponent)
    where T1 : struct where T2 : struct;
