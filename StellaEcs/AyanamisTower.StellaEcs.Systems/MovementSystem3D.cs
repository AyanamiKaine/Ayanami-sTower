using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.Systems;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MovementSystem3D : ISystem
{
    public bool Enabled { get; set; } = true;

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query(typeof(Position3D), typeof(Velocity3D)).ToList())
        {
            var pos3D = entity.GetCopy<Position3D>();
            var vel3D = entity.GetCopy<Velocity3D>();

            pos3D.Value.X += vel3D.Value.X * deltaTime;
            pos3D.Value.Y += vel3D.Value.Y * deltaTime;
            pos3D.Value.Z += vel3D.Value.Z * deltaTime;

            entity.Set(pos3D);
        }
    }
}
