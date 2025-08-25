using AyanamisTower.StellaEcs.HighPrecisionMath;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Components
{
    // Stores the previous physics tick position for interpolation.
    public struct PreviousPosition3D
    {
        public Vector3Double Value;
        public PreviousPosition3D(Vector3Double v) { Value = v; }
    }
}
