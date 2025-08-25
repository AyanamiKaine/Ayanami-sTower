using AyanamisTower.StellaEcs.HighPrecisionMath;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Components
{
    // Position used by the renderer after interpolation.
    public struct RenderPosition3D
    {
        public Vector3Double Value;
        public RenderPosition3D(Vector3Double v) { Value = v; }
    }
}
