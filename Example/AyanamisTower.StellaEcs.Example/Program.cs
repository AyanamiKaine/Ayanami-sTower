using AyanamisTower.NihilEx.SDLWrapper;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine;
using System.Timers;

static class Program
{
    [STAThread]
    private static void Main()
    {

        Console.WriteLine("Starting SDL3 Callback Application Example...");

        var app = new App();

        // Use the SdlHost.RunApplication method, passing the instance methods
        // Note: SdlHost.RunApplication handles SDL_Init and SDL_Quit internally via the callbacks.
        int exitCode = SdlHost.RunApplication(
            app.Initialize,
            app.Update,
            app.HandleEvent,
            app.Cleanup
            );

        Console.WriteLine($"Application finished with exit code: {exitCode}");

        /*
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
        */
    }
}

