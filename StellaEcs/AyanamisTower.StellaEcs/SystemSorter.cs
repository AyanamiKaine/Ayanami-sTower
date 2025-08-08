using System;
using System.Reflection;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Handles the logic for sorting systems based on their attribute-defined dependencies.
/// This class builds a dependency graph and performs a topological sort.
/// </summary>
public static class SystemSorter
{
    /// <summary>
    /// Sorts a list of systems based on [UpdateAfter] and [UpdateBefore] attributes.
    /// </summary>
    /// <param name="systemsToSort">The collection of system instances to sort.</param>
    /// <returns>A new list containing the sorted systems.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a circular dependency is detected.</exception>
    public static List<ISystem> Sort(IEnumerable<ISystem> systemsToSort)
    {
        var systems = systemsToSort.ToList();
        if (systems.Count <= 1)
        {
            return systems;
        }

        // --- Graph Representation ---
        // The key is the system type, the value is a list of types it depends on (must run after).
        var dependencyGraph = new Dictionary<Type, List<Type>>();
        // The number of incoming edges for each node (system type).
        var inDegree = new Dictionary<Type, int>();
        // A map from a system type to the list of systems that depend on it.
        var dependents = new Dictionary<Type, List<Type>>();

        var systemTypeMap = systems.ToDictionary(s => s.GetType(), s => s);
        var allSystemTypes = new HashSet<Type>(systems.Select(s => s.GetType()));

        // --- 1. Initialize the Graph ---
        foreach (var systemType in allSystemTypes)
        {
            dependencyGraph[systemType] = new List<Type>();
            inDegree[systemType] = 0;
            dependents[systemType] = new List<Type>();
        }

        // --- 2. Build the Graph from Attributes ---
        foreach (var systemType in allSystemTypes)
        {
            // Handle [UpdateAfter(typeof(OtherSystem))] -> Edge from OtherSystem to this system
            var afterAttributes = systemType.GetCustomAttributes<UpdateAfterAttribute>(true);
            foreach (var attr in afterAttributes)
            {
                if (allSystemTypes.Contains(attr.TargetSystem))
                {
                    dependencyGraph[systemType].Add(attr.TargetSystem);
                    dependents[attr.TargetSystem].Add(systemType);
                }
            }

            // Handle [UpdateBefore(typeof(OtherSystem))] -> Edge from this system to OtherSystem
            var beforeAttributes = systemType.GetCustomAttributes<UpdateBeforeAttribute>(true);
            foreach (var attr in beforeAttributes)
            {
                if (allSystemTypes.Contains(attr.TargetSystem))
                {
                    dependencyGraph[attr.TargetSystem].Add(systemType);
                    dependents[systemType].Add(attr.TargetSystem);
                }
            }
        }

        // --- 3. Calculate In-Degrees ---
        foreach (var (systemType, dependencies) in dependencyGraph)
        {
            inDegree[systemType] = dependencies.Count;
        }

        // --- 4. Perform Topological Sort (Kahn's Algorithm) ---
        var queue = new Queue<Type>(allSystemTypes.Where(t => inDegree[t] == 0));
        var sortedList = new List<ISystem>();

        while (queue.Count > 0)
        {
            var currentType = queue.Dequeue();
            sortedList.Add(systemTypeMap[currentType]);

            // For each system that depends on the current one...
            foreach (var dependentType in dependents[currentType])
            {
                // Decrement its in-degree. If it becomes 0, it's ready to be processed.
                inDegree[dependentType]--;
                if (inDegree[dependentType] == 0)
                {
                    queue.Enqueue(dependentType);
                }
            }
        }

        // --- 5. Check for Cycles ---
        if (sortedList.Count < systems.Count)
        {
            var cycleNodes = systems.Select(s => s.GetType()).Except(sortedList.Select(s => s.GetType()));
            throw new InvalidOperationException("Circular dependency detected! The following systems form a cycle or depend on one: " +
                                                string.Join(", ", cycleNodes.Select(t => t.Name)));
        }

        return sortedList;
    }
}
