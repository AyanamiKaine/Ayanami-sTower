//css_assemblyname Core3D.Module

using System;
using System.Collections.Generic;
using AyanamisTower.NihilEx;
// Import the components defined in the other assembly for this mod
using Core3D.Components;
using Flecs.NET.Core;

namespace Core3D.Module // Use a specific namespace
{
    // The main logic class implementing IFlecsModule
    public class Core3DModule : IMod
    {
        public string Name => "Core 3D System";
        public string Author => "Ayanami";
        public string Version => "1.0.0"; // This mod's version
        public string Description => "Provides core 3D components";

        public IReadOnlyList<ModDependency> Dependencies =>
            new List<ModDependency>
            {
                new ModDependency("Core Health System", ">=1.0.0"),
            }.AsReadOnly();
        public int LoadPriority => 0;
        public IReadOnlyList<string> Tags => new List<string> { "Core", "Gameplay" }.AsReadOnly();

        // This method is called by Flecs when the module is imported
        public void InitModule(World world)
        {
            world.Module<Core3DModule>();
            Console.WriteLine("Loading Core3DModule...");

            Console.WriteLine("Core3DModule Loaded.");
        }
    }
}
