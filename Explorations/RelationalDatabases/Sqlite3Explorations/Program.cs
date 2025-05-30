using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

using var db = new EntityContext();
await db.Database.EnsureCreatedAsync();
// Note: This sample requires the database to be created before running.
Console.WriteLine($"Database path: {db.DbPath}.");

db.Entities.RemoveRange(db.Entities);
await db.SaveChangesAsync();

// Create
Console.WriteLine("Inserting a new entity");

for (int i = 0; i < 10000; i++)
{
    db.Add(new Entity
    {
        Name = $"Block{i}",
        Position2DComponent = new Position2D { X = 10.0f, Y = 20.0f },
        Velocity2DComponent = new Velocity2D { X = 1.0f, Y = -0.5f }
    });
}
var e = new Entity() { Name = "ParentA" };

db.Add(new Entity() { Name = "ChildA", Parent = e });
db.Add(new Entity() { Name = "ChildB", Parent = e });

await db.SaveChangesAsync();
// Read
Console.WriteLine("Querying for a entity");

var result = await db.Entities
    .OrderBy(b => b.EntityId)
    .FirstAsync();

Console.WriteLine(result);

Stopwatch stopwatch = new();

Console.WriteLine("Starting measurement...");

stopwatch.Start();

foreach (var entity in db.Entities)
{
    var pos2D = entity.Get<Position2D>();
    var velocity2D = entity.Get<Velocity2D>();

    if (pos2D is not null || velocity2D is not null)
    {
        pos2D!.X += velocity2D!.X;
        pos2D!.Y += velocity2D!.Y;
    }
}

stopwatch.Stop();

Console.WriteLine("Measurement finished.");

// 5. Get and display the elapsed time
TimeSpan ts = stopwatch.Elapsed;

// Format and display the TimeSpan result
string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:000}",
    ts.Hours, ts.Minutes, ts.Seconds,
    ts.Milliseconds); // More precise than ts.TotalMilliseconds for display

Console.WriteLine($"Elapsed Time: {elapsedTime}");
Console.WriteLine($"Elapsed Milliseconds: {stopwatch.ElapsedMilliseconds} ms");
Console.WriteLine($"Elapsed Ticks: {stopwatch.ElapsedTicks} ticks");

await db.SaveChangesAsync();

var rootWithChildren = await db.Entities
    .Include(e => e.Children)            // Load direct children
    .ThenInclude(c => c.Children)    // For each child, load their children (grandchildren of root)
    .FirstOrDefaultAsync(e => e.ParentId == null); // Get top-level node(s)
