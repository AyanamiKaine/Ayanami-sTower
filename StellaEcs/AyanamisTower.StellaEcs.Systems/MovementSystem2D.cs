using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.Systems;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MovementSystem2D : ISystem
{
    public bool Enabled { get; set; } = true;

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query(typeof(Position2D), typeof(Velocity2D)).ToList())
        {
            var pos2D = entity.GetCopy<Position2D>();
            var vel2D = entity.GetCopy<Velocity2D>();

            pos2D.Value.X += vel2D.Value.X * deltaTime;
            pos2D.Value.Y += vel2D.Value.Y * deltaTime;

            entity.Set(pos2D);
        }
    }
}
