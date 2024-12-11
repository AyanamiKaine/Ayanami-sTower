using Avalonia.Threading;
using Flecs.NET.Bindings;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// Various helper methods for the ECS World
    /// </summary>
    public static class ECSWorldExtensionMethods
    {
        /// <summary>
        /// Here we progress the ecs world every defined interval
        /// in the UI Thread async and add the Ecs.Stats
        /// and set the EcsRest Singleton for the REST API to work.
        /// We are doing this to correctly
        /// do periodic updates in the ECS world to see its statistics
        /// here: https://flecs.dev/explorer
        /// If you run world.Progress() yourself its NOT RECOMMENDED to run this
        /// function.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static World RunRESTAPI(this World world, TimeSpan interval)
        {
            world.Import<Ecs.Stats>();
            world.Set(default(flecs.EcsRest));
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                PeriodicTimer timer = new(interval);
                while (await timer.WaitForNextTickAsync())
                {
                    world.Progress();
                };
            });
            return world;
        }

        /// <summary>
        /// Here we progress the ecs world every 25ms and add the Ecs.Stats
        /// and set the EcsRest Singleton for the REST API to work.
        /// We are doing this to correctly
        /// do periodic updates in the ECS world to see its statistics
        /// here: https://flecs.dev/explorer
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public static World RunRESTAPI(this World world)
        {
            world.Import<Ecs.Stats>();
            world.Set(default(flecs.EcsRest));
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                PeriodicTimer timer = new(TimeSpan.FromMilliseconds(25));
                while (await timer.WaitForNextTickAsync())
                {
                    world.Progress();
                };
            });
            return world;
        }
    }

}