using Flecs.NET.Core;

namespace StellaInvicta.Module
{
    public struct WorldSingletons : IFlecsModule
    {
        public readonly void InitModule(World world)
        {
            world.Set<EventDatabase>(new());
        }
    }
}