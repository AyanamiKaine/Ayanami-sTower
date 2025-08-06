using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a system that is executed before the main update loop.
/// </summary>
public interface IPreUpdateSystem
{
    /// <summary>
    /// This method is called before the main update loop to perform any necessary pre-update logic.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="deltaTime"></param>
    void PreUpdate(World world, float deltaTime);
}
