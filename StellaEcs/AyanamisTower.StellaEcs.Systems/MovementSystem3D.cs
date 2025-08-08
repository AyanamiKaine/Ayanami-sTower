using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.CorePlugin;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MovementSystem3D : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "MovementSystem3D";
    public int Priority { get; set; } = 0;
    public List<string> Dependencies => [];

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.Query(typeof(Position3D), typeof(Velocity3D)).ToList())
        {
            var pos3D = entity.GetCopy<Position3D>();
            var vel3D = entity.GetCopy<Velocity3D>();

            pos3D.Value += vel3D.Value * deltaTime;

            entity.Set(pos3D);
        }
    }
}
