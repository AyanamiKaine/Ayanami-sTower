using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents the geometry of a mesh with vertices and edges.
/// </summary>
/// <param name="BaseVertices">The base vertices of the mesh.</param>
/// <param name="Edges">The edges connecting vertices in the mesh.</param>
public record struct MeshGeometry(Vector3[] BaseVertices, Edge[] Edges);
