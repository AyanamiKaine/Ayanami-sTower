# Iteration Safety Guidelines

## Overview

The StellaEcs system uses sparse set storage with swap-to-end removal, which can cause iteration invalidation when components are modified during iteration.

## Unsafe Patterns

### ❌ Modifying components during View iteration

```csharp
var view = world.GetView<Health>();
foreach (var entity in view)
{
    if (view.Get(entity).Value <= 0)
    {
        world.RemoveComponent<Health>(entity); // UNSAFE! Invalidates iteration
    }
}
```

### ❌ Adding/removing entities during iteration

```csharp
var query = world.Query().With<Transform>();
foreach (var entity in query)
{
    if (SomeCondition())
    {
        world.CreateEntity(); // UNSAFE! May invalidate storage
        world.DestroyEntity(entity); // UNSAFE! May invalidate storage
    }
}
```

## Safe Patterns

### ✅ Collect entities first, then modify

```csharp
var view = world.GetView<Health>();
var entitiesToRemove = new List<Entity>();

foreach (var entity in view)
{
    if (view.Get(entity).Value <= 0)
    {
        entitiesToRemove.Add(entity);
    }
}

// Safe to modify after iteration
foreach (var entity in entitiesToRemove)
{
    world.RemoveComponent<Health>(entity);
}
```

### ✅ Use reverse iteration for removals

```csharp
var view = world.GetView<Health>();
for (int i = view.Count - 1; i >= 0; i--)
{
    var entity = view.GetEntity(i);
    if (view.GetByIndex(i).Value <= 0)
    {
        world.RemoveComponent<Health>(entity); // Safe with reverse iteration
    }
}
```

### ✅ Use QueryEnumerable for safer iteration

```csharp
var query = world.Query().With<Health>();
foreach (var entity in query)
{
    // Safer due to IsAlive() checks, but still avoid structural changes
    if (entity.Get<Health>().Value <= 0)
    {
        // Collect for later processing instead
    }
}
```

## Implementation Recommendations

### 1. Add Version Tracking

Consider adding version numbers to component storages to detect modification during iteration:

```csharp
public class ComponentStorage<T>
{
    private int _version = 0;

    public void Add(int entityId, T component)
    {
        // ... existing logic
        _version++;
    }

    public void Remove(int entityId)
    {
        // ... existing logic
        _version++;
    }
}
```

### 2. Add Debug Assertions

Add debug checks in View enumerators:

```csharp
public struct ViewEnumerator
{
    private readonly int _initialVersion;

    public bool MoveNext()
    {
        Debug.Assert(_storage.Version == _initialVersion,
            "Component storage was modified during iteration!");
        // ... rest of logic
    }
}
```

### 3. Consider Deferred Operations

Implement a command buffer system for deferred structural changes:

```csharp
public class DeferredCommandBuffer
{
    private List<Action> _commands = new();

    public void AddComponent<T>(Entity entity, T component)
    {
        _commands.Add(() => entity.Add(component));
    }

    public void Execute()
    {
        foreach (var command in _commands)
            command();
        _commands.Clear();
    }
}
```

## Best Practices

1. **Never modify component storage during iteration** unless using safe patterns
2. **Prefer collecting entities and processing them after iteration**
3. **Use reverse iteration when removing items**
4. **Consider using QueryEnumerable over Views for complex scenarios**
5. **Implement version checking in debug builds**
6. **Use deferred command buffers for complex structural changes**

## Current System Safety Level: ⚠️ MODERATE

-   QueryEnumerable: Partially safe (checks IsAlive)
-   Views: Unsafe for modifications during iteration
-   Recommendation: Implement the suggested safety measures
