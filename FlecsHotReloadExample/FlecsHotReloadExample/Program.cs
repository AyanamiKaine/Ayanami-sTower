using Flecs.NET.Core;

var world = World.Create();
world.Import<Ecs.Stats>();

world.Entity()
     .Set(5);

/* 
 * We cannot hotreload the underlying query logic.
 * But we can hotreload the callback that runs in the 
 * query. Try it change the logic and see how a different
 * value gets printed.
 */
world.System<int>()
     .Interval(1) //So the system runs only every second
     .Each((Entity entity, ref int e) =>
     {
        entity.Set(e);
        Console.WriteLine(e);
     });

/* But when we create a filewatcher that re-executes the world.System("SYSTEM_ID")
 * with the same system id we would overwrite the underlying query logic too!
 * 
 * We just need a simply hook to execute when we hotreload. There is no real clean way 
 * to do that besides, creating a filewatcher and manually watch for changes.
 * 
 * We could create a filewatcher to looks for changes in files and uses the filenames as an
 * an indicator what system needs to re-run. We could make it even dead simple. Create a json
 * file with the needed metadata. It holds a list of names for systems and if a name changes
 * reload that system. Could be a simple key value store. Or maybe just use flecs script?
 */
world.App().EnableRest().TargetFps(60).Run();
