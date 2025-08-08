using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Attribute to specify that a system should be updated after another system.
/// </summary>
/// <remarks>
/// Gets the type of the target system that this attribute is associated with.
/// </remarks>
/// <param name="targetSystem"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UpdateAfterAttribute(Type targetSystem) : Attribute
{
    /// <summary>
    /// Gets the type of the target system that this attribute is associated with.
    /// </summary>
    public Type TargetSystem { get; } = targetSystem;
}