using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Attribute to specify that a system should be updated before another system.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UpdateBeforeAttribute"/> class.
/// </remarks>
/// <param name="targetSystem"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UpdateBeforeAttribute(Type targetSystem) : Attribute
{
    /// <summary>
    /// Gets the type of the target system that this attribute is associated with.
    /// </summary>
    public Type TargetSystem { get; } = targetSystem;
}
