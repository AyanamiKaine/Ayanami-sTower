using Flecs.NET.Bindings;
using Flecs.NET.Core;

namespace FlecsExploration
{
    /// <summary>
    /// Flecs has a mature REST API implemented for 
    /// interacting with a flecs world over a network.
    /// 
    /// Seeing statistics, seeing all systems, components, entities, etc.
    /// 
    /// And being able to interact with them.
    /// You can add components to entities, remove them, or setting
    /// their values.
    /// </summary>
    public class REST
    {

        public record struct Position2D(double X, double Y);
        public record struct Velocity2D(double X, double Y);

        [Fact]
        public void EnableRESTAPI()
        {
            World world = World.Create();

            world.Component<Position2D>()
                .Member<double>("X")
                .Member<double>("Y");

            world.Component<Velocity2D>()
                .Member<double>("X")
                .Member<double>("Y");

            world.Import<Ecs.Stats>(); // Collect statistics periodically


            var entity = world.Entity("Entity")
                .Set<Position2D>(new(0, 0))
                .Set<Velocity2D>(new(1, 1));

            var system = world.System<Position2D, Velocity2D>()
                .Each((ref Position2D pos, ref Velocity2D vel) =>
                {
                    pos.X += vel.X;
                    pos.Y += vel.Y;
                });

            // Run application with REST interface. When the application is running,
            // navigate to https://flecs.dev/explorer to inspect it!
            //
            // See docs/FlecsRemoteApi.md#explorer for more information.
            // world.App().EnableRest().Run();

            // Alternatively you can run your own loop by setting the EcsRest singleton
            // and calling World.Progress().
            // world.Set(default(flecs.EcsRest));
            // while (world.Progress()) { }
        }
    }
}