using Flecs.NET;
using Flecs.NET.Core;

namespace HotReloadingTest;


public record struct Velocity2D(float X, float Y);
public record struct Position2D(float X, float Y);

public class System
{
    public string Name { get; set; } = string.Empty;
    
}

public static class Program
{

    public static World world = World.Create();
    public static void Main()
    {
        var entity = world.Entity("e1")
            .Set(new Position2D { X = 10, Y = 20 })
            .Set(new Velocity2D { X = 1, Y = 2 });

        System<Position2D, Velocity2D> system = world.System<Position2D, Velocity2D>("Speed")
            .Each(SystemFunction);
        while (true)
        {
            world.Progress();
            Thread.Sleep(2000); // Simulate some work or game tick
        }
    }
    // You can change the code of a running system.
    static void SystemFunction(Entity e, ref Position2D p, ref Velocity2D v)
    {
        p.X += v.X;
        p.Y += v.Y;
        Console.WriteLine($"{e} ({p.X}, {p.Y})");
    }
}