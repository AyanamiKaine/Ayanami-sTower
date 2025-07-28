using AyanamisTower.StellaEcs;

record struct Position2D(int X, int Y);


static class Program
{
    private static void Main()
    {
        var world = new World();

        var e1 = world.CreateEntity(); e1.Add(new Position2D { X = 10, Y = 0 });
        var e2 = world.CreateEntity(); e2.Add(new Position2D { X = 50, Y = 0 });
        var e3 = world.CreateEntity(); e3.Add(new Position2D { X = 100, Y = 0 });

        // Act: Find all entities with Position.X > 30
        var query = world.Query()
            .With<Position2D>()
            .Where<Position2D>(p => p.X > 30)
            .Build();

        var results = new List<Entity>();
        foreach (var e in query) Console.WriteLine(e);
    }
}