using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Specifies which SystemGroup a system belongs to.
/// If this attribute is not present on a system, it will be placed in the default
/// SimulationSystemGroup.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UpdateInGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the type of the target system group that this attribute is associated with.
    /// </summary>
    public Type TargetGroup { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateInGroupAttribute"/> class.
    /// </summary>
    /// <param name="targetGroup"></param>
    /// <exception cref="ArgumentException"></exception>
    public UpdateInGroupAttribute(Type targetGroup)
    {
        // Ensure the provided type is actually a SystemGroup
        if (!typeof(SystemGroup).IsAssignableFrom(targetGroup))
        {
            throw new ArgumentException($"{targetGroup.Name} is not a valid SystemGroup.", nameof(targetGroup));
        }
        TargetGroup = targetGroup;
    }
}
