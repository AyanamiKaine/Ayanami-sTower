using Flecs.NET.Core;
using Flecs.NET.Bindings;

namespace FlecsExploration
{
    public class Systems
    {
        public record struct Position2D(double X, double Y);
        public record struct Velocity2D(double X, double Y);
        [Fact]
        public void CreatingSystem()
        {
            World world = World.Create();
            var system = world.System<Position2D>()
                .Each((ref Position2D position) =>
                {
                    position.X += 1;
                    position.Y += 1;
                });
        }


        /// <summary>
        /// Sometimes we dont want to run a system when an entity has a certain component.
        /// saying something like "not with that component"
        /// 
        /// This is helpful when we want to run a system on all other entities but not on the player.
        /// 
        /// Or other components that often get dynamically added and removed.
        /// like ally, hostile, atWar etc.
        /// </summary>
        [Fact]
        public void SayingNotWithThatComponent()
        {
            World world = World.Create();
            var entity = world.Entity()
                .Set<Position2D>(new(0, 0));

            var system = world.System<Position2D>()
                .Without<Velocity2D>()
                .Each((ref Position2D position) =>
                {
                    position.X += 1;
                    position.Y += 1;
                });

            world.Progress();

            var position = entity.Get<Position2D>();
            Assert.Equal(1, position.X);
            Assert.Equal(1, position.Y);
        }
    }
}
