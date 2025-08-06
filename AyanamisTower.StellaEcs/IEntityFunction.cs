using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Defines a contract for a function that can be registered with the World
/// and invoked by name on a specific entity.
/// </summary>
public interface IEntityFunction
{
    /// <summary>
    /// Executes the function's logic on a given entity.
    /// </summary>
    /// <param name="target">The entity to perform the action on.</param>
    /// <param name="world">A reference to the world, providing context.</param>
    /// <param name="parameters">An array of parameters passed to the function.</param>
    void Execute(Entity target, World world, object[] parameters);
}