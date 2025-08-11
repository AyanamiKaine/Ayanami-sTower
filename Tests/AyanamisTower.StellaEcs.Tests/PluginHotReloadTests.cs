using System;
using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;

// This test exercises the hot-reload behavior: after unloading, old systems should be fully removed
// (both managed and unmanaged lists) so re-registering does not create duplicates.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class PluginHotReloadTests
{
    // Simulation group systems for the first registration (owner: World)
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    private class Movement2D_WorldFirst : ISystem
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "Core.MovementSystem2D";
        public void Update(World world, float deltaTime) { }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    private class Movement3D_WorldFirst : ISystem
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "Core.MovementSystem3D";
        public void Update(World world, float deltaTime) { }
    }

    // Simulation group systems for the second registration (owner: Core plugin)
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    private class Movement2D_CoreSecond : ISystem
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "Core.MovementSystem2D";
        public void Update(World world, float deltaTime) { }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    private class Movement3D_CoreSecond : ISystem
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "Core.MovementSystem3D";
        public void Update(World world, float deltaTime) { }
    }

    private class TestPlugin : IPlugin
    {
        public string Name => "Core Features (Test)";
        public string Version => "1.0.0";
        public string Author => "Tests";
        public string Description => "Test plugin for hot-reload behavior";
        public string Prefix => "Core";

        public IEnumerable<Type> ProvidedSystems => new[] { typeof(Movement2D_CoreSecond), typeof(Movement3D_CoreSecond) };
        public IEnumerable<Type> ProvidedServices => Array.Empty<Type>();
        public IEnumerable<Type> ProvidedComponents => Array.Empty<Type>();

        public void Initialize(World world)
        {
            // Register with owner mapping (this simulates the reloaded plugin adding its systems)
            world.RegisterSystem(new Movement2D_CoreSecond(), this);
            world.RegisterSystem(new Movement3D_CoreSecond(), this);
        }

        public void Uninitialize(World world)
        {
            // Remove by name
            world.RemoveSystemByName("Core.MovementSystem2D");
            world.RemoveSystemByName("Core.MovementSystem3D");
        }
    }

    [Fact]
    public void HotReload_ListSystems_ShouldOnlyContainNewPluginSystems()
    {
        var world = new World();

        // 1) First registration without owner (these will be seen as "World")
        world.RegisterSystem(new Movement2D_WorldFirst());
        world.RegisterSystem(new Movement3D_WorldFirst());

        // 2) Simulate plugin uninitialize: remove systems by name (should remove from both lists)
        world.RemoveSystemByName("Core.MovementSystem2D");
        world.RemoveSystemByName("Core.MovementSystem3D");

        // 3) Second registration with an owning plugin ("Core")
        var plugin = new TestPlugin();
        plugin.Initialize(world);

        // Force sorting so GetSystemsWithOrder reflects current state
        world.SortAndGroupSystems();

        // Produce the flattened representation: Name, Group (trimmed), #index, Owner
        static string TrimGroup(string groupName) => groupName.EndsWith("SystemGroup", StringComparison.Ordinal)
            ? groupName[..^"SystemGroup".Length]
            : groupName;

        var systems = world.GetSystemsWithOrder().ToList();
        var lines = new List<string>();
        for (int i = 0; i < systems.Count; i++)
        {
            var (system, group, orderIndex) = systems[i];
            var owner = world.GetSystemOwner(system.GetType())?.Prefix ?? "World";
            lines.Add(system.Name);
            lines.Add(TrimGroup(group.Name));
            lines.Add($"#{orderIndex}");
            lines.Add(owner);
            if (i < systems.Count - 1)
            {
                lines.Add("Disable");
            }
        }

        var flattened = string.Join(Environment.NewLine, lines);

        // Expect only two systems in Simulation group, owned by Core, ordered #0 and #1
        const string expected = "Core.MovementSystem2D\n" +
                                "Simulation\n" +
                                "#0\n" +
                                "Core\n" +
                                "Disable\n" +
                                "Core.MovementSystem3D\n" +
                                "Simulation\n" +
                                "#1\n" +
                                "Core";

        Assert.Equal(expected, flattened.Replace("\r\n", "\n"));
    }
}
