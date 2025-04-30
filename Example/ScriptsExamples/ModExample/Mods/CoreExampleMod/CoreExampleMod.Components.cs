//css_assemblyname Core3D.Components

// HealthMod/Components.cs
using System;
using Flecs.NET.Core; // Might be needed if components implement interfaces from Flecs

namespace Core3D.Components // Use a specific namespace
{
    public record struct Position(float X, float Y);

    public record struct Velocity(float X, float Y);
}
