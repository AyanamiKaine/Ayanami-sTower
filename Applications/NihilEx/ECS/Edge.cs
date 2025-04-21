namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents an edge connecting two vertices by index
/// </summary>
/// <param name="Index1">The index of the first vertex</param>
/// <param name="Index2">The index of the second vertex</param>
public record struct Edge(int Index1, int Index2);