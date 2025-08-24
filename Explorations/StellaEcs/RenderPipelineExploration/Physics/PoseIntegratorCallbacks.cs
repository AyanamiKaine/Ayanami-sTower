using System.Numerics;
using BepuPhysics;
using BepuUtilities;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public Vector3 Gravity;
    public PoseIntegratorCallbacks(Vector3 gravity) : this() { Gravity = gravity; }
    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public bool AllowSubstepsForUnconstrainedBodies => false;
    public bool IntegrateVelocityForKinematics => false;
    public void Initialize(Simulation simulation) { }
    public void PrepareForIntegration(float dt) { }
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        // Broadcast scalar gravity to a wide vector and scale by the per-lane timestep.
        Vector3Wide.Broadcast(Gravity, out var gravityWide);
        Vector3Wide.Scale(gravityWide, dt, out var gravityDt);
        velocity.Linear += gravityDt;
    }
}
