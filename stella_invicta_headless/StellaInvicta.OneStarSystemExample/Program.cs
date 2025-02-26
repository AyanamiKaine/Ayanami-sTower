using Flecs.NET.Core;
using StellaInvicta;
using StellaInvicta.Components;
using StellaInvicta.Systems;

var world = World.Create();
world.Import<StellaInvictaECSModule>();
world.AddSystem(new AgeSystem());

var Marina = world.Entity("Marina")
    .Set<Name>(new("Marina"))
    .Set<Age>(new(29));