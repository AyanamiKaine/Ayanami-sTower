using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;



using System.Timers;

static class Program
{
    private static void Main()
    {
        var world = new World();


        var pluginLoader = new HotReloadablePluginLoader(world, "Plugins");

        // 3. Load all plugins that already exist in the folder at startup.
        pluginLoader.LoadAllExistingPlugins();

        // 4. Start watching for any new plugins or changes.
        pluginLoader.StartWatching();

        world.CreateEntity()
            .Set(new Position2D(0, 0))
            .Set(new Velocity2D(1, 1));

        world.EnableRestApi();

        System.Timers.Timer timer = new(1000.0 / 60); // 60 FPS
        timer.Elapsed += (sender, e) =>
        {
            world.Update(1f / 60f); // Update with delta time
        };

        timer.AutoReset = true;
        timer.Start();

        while (true)
        {
            // Keep the application running
            Thread.Sleep(1000);
        }
    }
}

