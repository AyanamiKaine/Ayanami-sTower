using ScriptManager;
using Flecs.NET;
using Flecs.NET.Core;

World world = World.Create();
NamedEntities entities = new(world);

ScriptHandler handler = new(world, entities);
handler.CompileScriptsFromFolder("./scripts");
handler.RunScriptAsync("helloWorld").Wait();