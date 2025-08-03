using System;
using System.Runtime.CompilerServices;
using AyanamisTower.StellaEcs.Attributes;

namespace AyanamisTower.StellaEcs.Extensions;

/// <summary>
/// Core extension methods that provide the foundation for component-based entity operations.
/// These methods enable ergonomic access to multiple components simultaneously.
/// </summary>
public static class EntityComponentExtensions
{
    /// <summary>
    /// Executes an action with the specified components from the entity.
    /// This is the core method that other extensions build upon.
    /// </summary>
    /// <typeparam name="T1">First component type</typeparam>
    /// <param name="entity">The entity to operate on</param>
    /// <param name="action">Action to execute with the component</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void With<T1>(this Entity entity, Action<T1> action)
        where T1 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");

        action(entity.Get<T1>());
    }

    /// <summary>
    /// Executes an action with two components from the entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void With<T1, T2>(this Entity entity, Action<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");
        if (!entity.Has<T2>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T2).Name}");

        action(entity.Get<T1>(), entity.Get<T2>());
    }

    /// <summary>
    /// Executes an action with three components from the entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void With<T1, T2, T3>(this Entity entity, Action<T1, T2, T3> action)
        where T1 : struct where T2 : struct where T3 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");
        if (!entity.Has<T2>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T2).Name}");
        if (!entity.Has<T3>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T3).Name}");

        action(entity.Get<T1>(), entity.Get<T2>(), entity.Get<T3>());
    }

    /// <summary>
    /// Executes an action with mutable access to the specified component.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithMutable<T1>(this Entity entity, RefAction<T1> action)
        where T1 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");

        action(ref entity.GetMutable<T1>());
    }

    /// <summary>
    /// Executes an action with mutable access to two components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithMutable<T1, T2>(this Entity entity, RefAction<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");
        if (!entity.Has<T2>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T2).Name}");

        action(ref entity.GetMutable<T1>(), ref entity.GetMutable<T2>());
    }

    /// <summary>
    /// Executes a function and returns a result using the specified component.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult With<T1, TResult>(this Entity entity, Func<T1, TResult> func)
        where T1 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");

        return func(entity.Get<T1>());
    }

    /// <summary>
    /// Executes a function and returns a result using two components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult With<T1, T2, TResult>(this Entity entity, Func<T1, T2, TResult> func)
        where T1 : struct where T2 : struct
    {
        if (!entity.Has<T1>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T1).Name}");
        if (!entity.Has<T2>())
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T2).Name}");

        return func(entity.Get<T1>(), entity.Get<T2>());
    }

    /// <summary>
    /// Safely executes an action only if the entity has the required component.
    /// Returns true if the action was executed, false otherwise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWith<T1>(this Entity entity, Action<T1> action)
        where T1 : struct
    {
        if (!entity.Has<T1>()) return false;
        action(entity.Get<T1>());
        return true;
    }

    /// <summary>
    /// Safely executes an action only if the entity has all required components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWith<T1, T2>(this Entity entity, Action<T1, T2> action)
        where T1 : struct where T2 : struct
    {
        if (!entity.Has<T1>() || !entity.Has<T2>()) return false;
        action(entity.Get<T1>(), entity.Get<T2>());
        return true;
    }

    /// <summary>
    /// Checks if the entity has all the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAll<T1, T2>(this Entity entity)
        where T1 : struct where T2 : struct
    {
        return entity.Has<T1>() && entity.Has<T2>();
    }

    /// <summary>
    /// Checks if the entity has all the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAll<T1, T2, T3>(this Entity entity)
        where T1 : struct where T2 : struct where T3 : struct
    {
        return entity.Has<T1>() && entity.Has<T2>() && entity.Has<T3>();
    }

    /// <summary>
    /// Checks if the entity has any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAny<T1, T2>(this Entity entity)
        where T1 : struct where T2 : struct
    {
        return entity.Has<T1>() || entity.Has<T2>();
    }
}

/// <summary>
/// Delegate for actions that take ref parameters (for mutable component access).
/// </summary>
public delegate void RefAction<T1>(ref T1 component1) where T1 : struct;

/// <summary>
/// Delegate for actions that take two ref parameters.
/// </summary>
public delegate void RefAction<T1, T2>(ref T1 component1, ref T2 component2)
    where T1 : struct where T2 : struct;
