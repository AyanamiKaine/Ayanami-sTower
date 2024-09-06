using Flecs.NET.Bindings;
using Flecs.NET.Core;

namespace StellaInvicta.Module
{

    public struct Game : IFlecsModule
    {

        public readonly void InitModule(World world)
        {
            world.Import<Ecs.Units>();
            world.Import<Ecs.Stats>();
            world.Import<Components>();
            world.Import<WorldSingletons>();
            world.Import<PreDefinedEntities>();
            world.Import<Systems>();
            world.Set(default(flecs.EcsRest));
        }
    }
}