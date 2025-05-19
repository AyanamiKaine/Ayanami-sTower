using System;
using MagicOnion;
namespace AyanamisTower.StellaInvicta.Shared;


/// <summary>
/// The interface is shared between server and client.
/// </summary>
public interface IMyFirstService : IService<IMyFirstService>
{
    /// <summary>
    /// The return type must be `UnaryResult T` or `UnaryResult`.
    /// </summary>
    UnaryResult<int> SumAsync(int x, int y);
}
