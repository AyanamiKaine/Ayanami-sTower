using System;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// A simple ISystem implementation that delegates its Update to a provided Action.
/// Useful for quick prototyping and tests.
/// </summary>
public class DelegateSystem : ISystem
{
    private readonly Action<World, float> _update;

    /// <summary>
    /// Optional list of name-based dependencies. If provided, the sorter will require
    /// all named systems to be present and will run them before this system.
    /// </summary>
    /// <summary>
    /// Optional list of name-based dependencies. Sorter ensures these systems run before this one.
    /// </summary>
    public IEnumerable<string>? Dependencies { get; init; }

    /// <inheritdoc />
    public string Name { get; set; }
    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Constructs a delegate-backed system.
    /// </summary>
    /// <param name="name">Unique system name.</param>
    /// <param name="update">Callback invoked each frame.</param>
    /// <param name="dependencies">Optional name-based dependencies.</param>
    public DelegateSystem(string name, Action<World, float> update, IEnumerable<string>? dependencies = null)
    {
        Name = name;
        _update = update ?? throw new ArgumentNullException(nameof(update));
        Dependencies = dependencies;
    }

    /// <summary>
    /// Invokes the provided delegate.
    /// </summary>
    /// <param name="world">World instance.</param>
    /// <param name="deltaTime">Delta time in seconds.</param>
    public void Update(World world, float deltaTime) => _update(world, deltaTime);
}
