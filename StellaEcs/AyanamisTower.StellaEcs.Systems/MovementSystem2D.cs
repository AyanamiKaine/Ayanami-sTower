using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.Systems;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MovementSystem2D : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "MovementSystem2D";
    public int Priority { get; set; } = 0;

    public List<string> Dependencies => [];

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query(typeof(Position2D), typeof(Velocity2D)).ToList())
        {
            var pos2D = entity.GetCopy<Position2D>();
            var vel2D = entity.GetCopy<Velocity2D>();

            pos2D.Value += vel2D.Value * deltaTime;

            entity.Set(pos2D);
        }
    }
}
