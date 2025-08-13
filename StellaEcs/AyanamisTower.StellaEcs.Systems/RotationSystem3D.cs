using System.Numerics;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.CorePlugin;

/// <summary>
/// Applies simple rotational motion to entities with Position3D and Rotation3D.
/// If an AngularVelocity3D dynamic (radians/sec per axis) is present, uses it; otherwise applies a default.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class RotationSystem3D : ISystem
{
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;
    /// <inheritdoc/>
    public string Name { get; set; } = "RotationSystem3D";
    /// <inheritdoc/>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Optional default angular velocity if none is provided as a dynamic component.
    /// </summary>
    public Vector3 DefaultAngularVelocity = new(0.7f, 1.0f, 0f);

    /// <inheritdoc/>
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query(typeof(Position3D), typeof(AngularVelocity3D), typeof(Rotation3D)).ToList())
        {
            // Pull rotation; if absent, skip (shouldn't happen due to query)
            var rot = entity.GetCopy<Rotation3D>();
            // Angular velocity source: try dynamic component first
            Vector3 av = entity.GetCopy<AngularVelocity3D>().Value;

            // Integrate yaw/pitch/roll in radians
            var incr = Quaternion.CreateFromYawPitchRoll(av.Y * deltaTime, av.X * deltaTime, av.Z * deltaTime);
            rot.Value = Quaternion.Normalize(incr * rot.Value);

            entity.Set(rot);
        }
    }
}
