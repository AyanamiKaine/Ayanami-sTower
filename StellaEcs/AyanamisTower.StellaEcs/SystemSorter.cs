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

        // Pre-check unresolved dependencies regardless of system count
        ISystem? FindByTypeLocal(Type t) => systems.FirstOrDefault(sys => sys.GetType() == t);
        ISystem? FindByNameLocal(string name) => systems.FirstOrDefault(sys => string.Equals(sys.Name, name, StringComparison.Ordinal));

        foreach (var sys in systems)
        {
            var unresolved = new List<string>();
            var t = sys.GetType();
            foreach (var attr in t.GetCustomAttributes<UpdateAfterAttribute>(true))
            {
                if (FindByTypeLocal(attr.TargetSystem) is null)
                {
                    unresolved.Add(attr.TargetSystem.Name);
                }
            }
            foreach (var attr in t.GetCustomAttributes<UpdateBeforeAttribute>(true))
            {
                if (FindByTypeLocal(attr.TargetSystem) is null)
                {
                    unresolved.Add(attr.TargetSystem.Name);
                }
            }
            var depsPropLocal = t.GetProperty("Dependencies", BindingFlags.Public | BindingFlags.Instance);
            if (depsPropLocal?.GetValue(sys) is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is string name && !string.IsNullOrWhiteSpace(name))
                    {
                        if (FindByNameLocal(name) is null)
                        {
                            unresolved.Add(name);
                        }
                    }
                }
            }

            if (unresolved.Count > 0)
            {
                throw new InvalidOperationException($"System '{sys.Name}' has unresolved dependency on '{string.Join("', '", unresolved)}'");
            }
        }

        if (systems.Count <= 1)
        {
            return systems;
        }

        // --- Graph Representation based on instances ---
        // We'll key everything by the system instance to correctly handle multiple systems of the same type.
        var dependencyGraph = new Dictionary<ISystem, List<ISystem>>(); // edges: A depends on B (B -> A), store B in list for A
        var inDegree = new Dictionary<ISystem, int>();
        var dependents = new Dictionary<ISystem, List<ISystem>>();

        // --- 1. Initialize the Graph ---
        foreach (var s in systems)
        {
            dependencyGraph[s] = new List<ISystem>();
            inDegree[s] = 0;
            dependents[s] = new List<ISystem>();
        }

        // Helper: find system instance by Type or by Name
        ISystem? FindByType(Type t) => systems.FirstOrDefault(sys => sys.GetType() == t);
        ISystem? FindByName(string name) => systems.FirstOrDefault(sys => string.Equals(sys.Name, name, StringComparison.Ordinal));

        // --- 2. Build the Graph from Attributes and optional Dependencies property ---
        foreach (var system in systems)
        {
            var systemType = system.GetType();
            // Handle [UpdateAfter(typeof(OtherSystem))] -> Edge from OtherSystem to this system
            var afterAttributes = systemType.GetCustomAttributes<UpdateAfterAttribute>(true);
            foreach (var attr in afterAttributes)
            {
                var targetInstance = FindByType(attr.TargetSystem);
                if (targetInstance != null)
                {
                    dependencyGraph[system].Add(targetInstance);
                    dependents[targetInstance].Add(system);
                }
            }

            // Handle [UpdateBefore(typeof(OtherSystem))] -> Edge from this system to OtherSystem
            var beforeAttributes = systemType.GetCustomAttributes<UpdateBeforeAttribute>(true);
            foreach (var attr in beforeAttributes)
            {
                var targetInstance = FindByType(attr.TargetSystem);
                if (targetInstance != null)
                {
                    dependencyGraph[targetInstance].Add(system);
                    dependents[system].Add(targetInstance);
                }
            }

            // Optional: property named "Dependencies" of type IEnumerable<string> to express name-based dependencies
            var depsProp = systemType.GetProperty("Dependencies", BindingFlags.Public | BindingFlags.Instance);
            if (depsProp != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(depsProp.PropertyType))
            {
                if (depsProp.GetValue(system) is System.Collections.IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is string depName && !string.IsNullOrWhiteSpace(depName))
                        {
                            var targetByName = FindByName(depName);
                            if (targetByName != null)
                            {
                                // Name dependency means: this system depends on target (target must run before this)
                                dependencyGraph[system].Add(targetByName);
                                dependents[targetByName].Add(system);
                            }
                        }
                    }
                }
            }
        }

        // --- 3. Calculate In-Degrees ---
        foreach (var (sys, dependencies) in dependencyGraph)
        {
            inDegree[sys] = dependencies.Count;
        }

        // --- 4. Perform Topological Sort (Kahn's Algorithm) ---
        var queue = new Queue<ISystem>(systems.Where(s => inDegree[s] == 0));
        var sortedList = new List<ISystem>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sortedList.Add(current);

            // For each system that depends on the current one...
            foreach (var dependentSys in dependents[current])
            {
                // Decrement its in-degree. If it becomes 0, it's ready to be processed.
                inDegree[dependentSys]--;
                if (inDegree[dependentSys] == 0)
                {
                    queue.Enqueue(dependentSys);
                }
            }
        }

        // --- 5. Check for Cycles ---
        if (sortedList.Count < systems.Count)
        {
            var cycleNodes = systems.Except(sortedList).Select(s => s.Name);
            throw new InvalidOperationException("Circular dependency detected! The following systems form a cycle or depend on one: " +
                                                string.Join(", ", cycleNodes));
        }

        // --- 6. Detect unresolved dependencies (attributes or name deps pointing to missing systems) ---
        // If a system declares non-empty dependencies but none of them were wired (because targets weren't present), throw.
        foreach (var system in systems)
        {
            int declaredDeps = 0;
            // Count attribute-based deps
            declaredDeps += system.GetType().GetCustomAttributes<UpdateAfterAttribute>(true).Count();
            declaredDeps += system.GetType().GetCustomAttributes<UpdateBeforeAttribute>(true).Count();
            // Name-based deps
            var depsProp = system.GetType().GetProperty("Dependencies", BindingFlags.Public | BindingFlags.Instance);
            if (depsProp?.GetValue(system) is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is string depName && !string.IsNullOrWhiteSpace(depName)) declaredDeps++;
                }
            }

            // If declared but none actually recorded in dependencyGraph[system], check whether some were unresolved
            if (declaredDeps > 0 && dependencyGraph[system].Count == 0 && dependents[system].Count == 0)
            {
                throw new InvalidOperationException($"System '{system.Name}' has unresolved dependency");
            }
        }

        return sortedList;
    }
}
