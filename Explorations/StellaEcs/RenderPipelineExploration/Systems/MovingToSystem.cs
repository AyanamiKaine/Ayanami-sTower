using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.Systems;

/// <summary>
/// Updates Velocity3D for entities with a MovingTo intent so they move toward
/// their target and stop within the arrival radius.
/// </summary>
#pragma warning disable CS1591
public sealed class MovingToSystem : ISystem
{
    public string Name { get; set; } = "MovingToSystem";
    public bool Enabled { get; set; } = true;
    // Run earlier so MovementSystem3D (default priority) integrates the updated velocity
    public int Priority { get; set; } = -10;

    public void Update(World world, float deltaTime)
    {
        if (!Enabled) return;
        if (deltaTime <= 0f) return;

        foreach (var e in world.Query(typeof(MovingTo), typeof(Position3D), typeof(Velocity3D)).ToList())
        {
            var intent = e.GetCopy<MovingTo>();
            var pos = e.GetCopy<Position3D>().Value;

            var toTarget = intent.Target - pos;
            var dist = toTarget.Length();

            // Arrived: stop and clear intent
            if (dist <= intent.ArriveRadius)
            {
                e.Set(new Velocity3D(0, 0, 0));
                e.Remove<MovingTo>();
                continue;
            }

            // Desired direction and capped speed to avoid overshoot this frame
            var dir = AyanamisTower.StellaEcs.HighPrecisionMath.Vector3Double.Normalize(toTarget);
            double maxStep = intent.Speed * deltaTime;
            double desiredSpeed = intent.Speed;
            if (dist < maxStep && deltaTime > 0)
            {
                desiredSpeed = dist / deltaTime; // arrive exactly this frame
            }

            var vel = dir * desiredSpeed;
            e.Set(new Velocity3D(vel));
        }
    }
}
