// HealthMod/Components.cs
using System;
using Flecs.NET.Core; // Might be needed if components implement interfaces from Flecs

namespace HealthMod.Components // Use a specific namespace
{
    // Simple component struct
    public record struct Health(float Current, float Max);

    public struct Mana
    {
        public float Current;
        public float Max;
    }

    // Tag component (empty struct)
    public struct IsRegeneratingHealth { }
}
