using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.Systems;

/// <summary>
/// Updates Velocity3D for entities with a Tracking component so they move toward
/// the current position of the tracked entity and stop within the arrival radius.
/// </summary>
public sealed class TrackingSystem : ISystem
{
    /// <inheritdoc/>
    public string Name { get; set; } = "TrackingSystem";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;
    /// <inheritdoc/>
    public int Priority { get; set; } = -10;

    /// <inheritdoc/>
    public void Update(World world, float deltaTime)
    {
        if (!Enabled) return;
        if (deltaTime <= 0f) return;

        // Query entities that are tracking and can move
        foreach (var e in world.Query(typeof(Tracking), typeof(Position3D), typeof(Velocity3D)).ToList())
        {
            var tracking = e.GetCopy<Tracking>();

            // If target is invalid or removed, cancel tracking
            if (!world.IsEntityValid(tracking.Target))
            {
                e.Remove<Tracking>();
                e.Set(new Velocity3D(0, 0, 0));
                continue;
            }

            // Target must have a Position3D to be trackable
            if (!tracking.Target.Has<Position3D>())
            {
                // Can't determine target position; stop movement
                e.Set(new Velocity3D(0, 0, 0));
                continue;
            }

            var targetPos = tracking.Target.GetCopy<Position3D>().Value;
            var pos = e.GetCopy<Position3D>().Value;

            var toTarget = targetPos - pos;
            var dist = toTarget.Length();


            /*   
            I dont think tracking should automatic stop when the target is within arriveRadius
            it instead should stay in the arrive radius stationed around the object it currently tracks.
            // Arrived: stop and clear intent
            if (dist <= tracking.ArriveRadius)
            {
                e.Set(new Velocity3D(0, 0, 0));
                e.Remove<Tracking>();
                continue;
            }
            */


            var dir = AyanamisTower.StellaEcs.HighPrecisionMath.Vector3Double.Normalize(toTarget);
            double maxStep = tracking.Speed * deltaTime;
            double desiredSpeed = tracking.Speed;
            if (dist < maxStep && deltaTime > 0)
            {
                desiredSpeed = dist / deltaTime;
            }

            var vel = dir * desiredSpeed;
            e.Set(new Velocity3D(vel));
        }
    }
}
