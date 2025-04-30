//css_assemblyname HealthMod.Module

using System;
using System.Collections.Generic;
using AyanamisTower.NihilEx;
using Flecs.NET.Core;
// Import the components defined in the other assembly for this mod
using HealthMod.Components;

namespace HealthMod.Module // Use a specific namespace
{
    // The main logic class implementing IFlecsModule
    public class HealthModule : IMod
    {
        public string Name => "Core Health System";
        public string Author => "Ayanami";
        public string Version => "1.0.0"; // This mod's version
        public string Description => "Provides basic health and mana components and regeneration.";

        public IReadOnlyList<ModDependency> Dependencies =>
            new List<ModDependency> { }.AsReadOnly();
        public int LoadPriority => 0;
        public IReadOnlyList<string> Tags =>
            new List<string> { "Core", "Gameplay", "Stats" }.AsReadOnly();

        public static string STATICVARIABLE = "TEST STATIC STRING";

        // This method is called by Flecs when the module is imported
        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Simple.Module)
            world.Module<HealthModule>();
            Console.WriteLine("Loading HealthModule...");

            // Register components explicitly (good practice in modules)
            world.Component<Health>().Member<float>("Current").Member<float>("Max");
            world.Component<Mana>();
            world.Component<IsRegeneratingHealth>();

            Console.WriteLine("HealthModule Loaded.");

            System<Health> system = world
                .System<Health>()
                .Each(
                    (Entity e, ref Health health) =>
                    {
                        Console.WriteLine($"{e}");
                    }
                );
        }
    }
}
