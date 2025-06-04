using System.Diagnostics;
using System.Transactions;
using AyanamisTower.StellaDB;
using AyanamisTower.StellaDB.Model;
using SqlKata.Execution;

var world = new World("Exploration", inMemory: false, enabledOptimizations: true);
var a = world.Entity("Entity1");
var b = world.Entity("Entity2");
var c = world.Entity("Entity3");
var d = world.Entity("Entity4");
var e = world.Entity("Entity5");

int numberOfRuns = 10000;
var entityIds = new[] { "Entity1", "Entity2", "Entity3", "Entity4", "Entity5" };

// Test 1: All operations in one transaction
var stopWatch = Stopwatch.StartNew();
using (var scope = new TransactionScope())
{
    for (int i = 0; i < numberOfRuns; i++)
    {
        world.Query("Age")
             .WhereIn("EntityId", entityIds)
             .Update(new { Value = 10 + i });
    }
    scope.Complete();
}
stopWatch.Stop();
Console.WriteLine($"One Transaction Total Time: {stopWatch.ElapsedMilliseconds} ms");
Console.WriteLine($"One Transaction Average: {(double)stopWatch.ElapsedTicks / numberOfRuns / TimeSpan.TicksPerMicrosecond:F2} μs per operation");

// Test 2: Each iteration in its own transaction
stopWatch.Restart();
for (int i = 0; i < numberOfRuns; i++)
{
    using var scope = new TransactionScope();
    a.Update<Age>(new() { Value = 10 + i });
    b.Update<Age>(new() { Value = 10 + i });
    c.Update<Age>(new() { Value = 10 + i });
    d.Update<Age>(new() { Value = 10 + i });
    e.Update<Age>(new() { Value = 10 + i });
    scope.Complete();
}
stopWatch.Stop();
Console.WriteLine($"Many Transactions Total Time: {stopWatch.ElapsedMilliseconds} ms");
Console.WriteLine($"Many Transactions Average: {(double)stopWatch.ElapsedTicks / numberOfRuns / TimeSpan.TicksPerMicrosecond:F2} μs per operation");

// Comparison
var ratio = (double)stopWatch.ElapsedTicks / numberOfRuns;
Console.WriteLine($"Performance difference: Many transactions are ~{ratio:F1}x slower per operation");