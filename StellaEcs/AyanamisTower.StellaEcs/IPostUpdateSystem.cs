using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a system that is executed after the main update loop.
/// </summary>
public interface IPostUpdateSystem
{
    /// <summary>
    /// This method is called after the main update loop to perform any necessary post-update logic.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="deltaTime"></param>
    void PostUpdate(World world, float deltaTime);
}
