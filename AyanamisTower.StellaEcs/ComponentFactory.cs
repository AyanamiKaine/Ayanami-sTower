using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Provides methods for dynamically creating ECS component types at runtime.
/// This class is now idempotent and caches the types it creates.
/// </summary>
public static class ComponentFactory
{
    private static readonly AssemblyName AssemblyName = new("RuntimeEcsComponents");
    private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule("MainModule");

    // FIX: Add a cache to store already created types.
    private static readonly Dictionary<string, Type> TypeCache = new();

    /// <summary>
    /// Creates a new struct type in memory with the specified fields.
    /// If a type with the same name has already been created, it returns the cached type.
    /// </summary>
    /// <param name="typeName">The name of the new component type (e.g., "HealthComponent").</param>
    /// <param name="fields">A dictionary where the key is the field name (e.g., "Value") and the value is the field type (e.g., typeof(int)).</param>
    /// <returns>The newly created or cached Type, which can be registered with the ECS World.</returns>
    public static Type CreateComponentType(string typeName, Dictionary<string, Type> fields)
    {
        // FIX: Check the cache first to prevent creating a duplicate type.
        if (TypeCache.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        // 1. Create a TypeBuilder for a public struct.
        //    Structs must be sealed and sequential layout is good practice for interop.
        TypeBuilder typeBuilder = ModuleBuilder.DefineType(typeName,
            TypeAttributes.Public |
            TypeAttributes.Sealed |
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass,
            typeof(ValueType)); // Inherit from ValueType to make it a struct.

        // 2. Define each field in the struct.
        foreach (var field in fields)
        {
            typeBuilder.DefineField(field.Key, field.Value, FieldAttributes.Public);
        }

        // 3. Finalize and create the type.
        Type newType = typeBuilder.CreateType()!;

        // FIX: Add the newly created type to the cache.
        TypeCache[typeName] = newType;

        return newType;
    }
}
