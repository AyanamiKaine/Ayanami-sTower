using System.Diagnostics;
using System.Transactions;
using AyanamisTower.StellaDB;
using AyanamisTower.StellaDB.Model;
using SqlKata.Execution;
using Entity = AyanamisTower.StellaDB.Entity;

List<Entity> entities = [];
List<long> entityIds = [];

if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exploration.db")))
    File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exploration.db"));

var world = new World("Exploration", inMemory: false, enabledOptimizations: true);

/*
The query will determine the performance, bad query == bad performance!
*/

int numberOfRuns = 10;
int numberOfEntities = 10000;
using (var scope = new TransactionScope())
{
    for (int i = 0; i < numberOfEntities; i++)
    {
        var e = world.Entity();
        e.Add<Age>(new() { Value = 10 });
        entities.Add(e);
        entityIds.Add(e.Id);
    }
    scope.Complete();
}
// Test 1: All operations in one transaction
var stopWatch = Stopwatch.StartNew();
using (var scope = new TransactionScope())
{
    for (int i = 0; i < numberOfRuns; i++)
    {
        world.Query("Age")
             .Increment("Value", 1);
    }
    stopWatch.Stop();
    scope.Complete();
}
Console.WriteLine($"One Query Total Time: {stopWatch.ElapsedMilliseconds} ms");
Console.WriteLine($"One Query Average: {(double)stopWatch.ElapsedTicks / numberOfRuns / TimeSpan.TicksPerMicrosecond:F2} μs per operation");

// Test 2: Each iteration in its own transaction
var stopWatch2 = Stopwatch.StartNew();

using var scope2 = new TransactionScope();

for (int i = 0; i < numberOfRuns; i++)
{
    foreach (var entity in entities)
    {
        entity.Update<Age>(new() { Value = 10 + i });
    }
}
stopWatch2.Stop();
scope2.Complete();

Console.WriteLine($"Many Queries Total Time: {stopWatch2.ElapsedMilliseconds} ms");
Console.WriteLine($"Many Queries Average: {(double)stopWatch2.ElapsedTicks / numberOfRuns / TimeSpan.TicksPerMicrosecond:F2} μs per operation");