using System;
using AyanamisTower.StellaInvicta.Shared;
using MagicOnion;
using MagicOnion.Server;

namespace AyanamisTower.StellaInvicta.Server.Services;

// Implements RPC service in the server project.
/// <summary>
/// The implementation class must inherit `ServiceBase IMyFirstService` and `IMyFirstService`
/// </summary>
public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
{
    /// <summary>
    /// `UnaryResult T` allows the method to be treated as `async` method.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public async UnaryResult<int> SumAsync(int x, int y)
    {
        Console.WriteLine($"Received:{x}, {y}");
        await Task.Delay(1);
        return x + y;
    }
}