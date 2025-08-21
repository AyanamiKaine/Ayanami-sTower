# Floating Origin System Implementation

## Overview

The floating origin system has been implemented to prevent floating point precision issues when dealing with large coordinates in your space simulation. This system automatically rebases all world coordinates by periodically subtracting a large offset from every object, physics object, and the world origin itself.

## Key Components

### 1. Vector3d Structure

-   **Purpose**: Double-precision 3D vector for storing large coordinate values
-   **Usage**: Used for absolute world positions that can be very large
-   **Features**:
    -   Implicit conversion to/from Vector3
    -   Standard vector operations (+, -, \*, /)
    -   Length() and Normalized() methods

### 2. AbsolutePosition Component

-   **Purpose**: Stores the true absolute position in the universe using double precision
-   **Usage**: Add this component to entities that need floating origin support
-   **Relationship**: Works alongside Position3D which stores relative position from current origin

### 3. FloatingOriginManager Class

-   **Purpose**: Manages the floating origin system and performs rebasing operations
-   **Key Features**:
    -   Monitors camera distance from origin
    -   Automatically triggers rebase when threshold is exceeded
    -   Updates all entities and physics objects during rebase
    -   Provides conversion between absolute and relative coordinates

## How It Works

1. **Entities store two positions**:

    - `Position3D`: Relative position from current floating origin (float precision)
    - `AbsolutePosition`: True world position (double precision)

2. **Automatic rebasing**:

    - When camera moves beyond threshold distance (default: 1000 units)
    - All entities and physics objects are shifted by the rebase offset
    - World origin is updated to new location

3. **Physics integration**:
    - Kinematic/dynamic bodies: Position updated directly
    - Static bodies: Removed and re-added with new position
    - All objects remain correctly positioned relative to each other

## Usage

### Automatic Setup

The system is automatically initialized and integrated into your Update loop:

```csharp
// In InitializeScene()
_floatingOriginManager = new FloatingOriginManager(World, _simulation, 1000.0);

// In Update()
_floatingOriginManager?.Update(_camera.Position);
```

### Adding Entities

For new entities that need floating origin support:

```csharp
var entity = World.CreateEntity()
    .Set(new Position3D(x, y, z))           // Relative position
    .Set(new AbsolutePosition(x, y, z))     // Absolute position
    // ... other components
```

The system automatically adds `AbsolutePosition` to entities that have `Position3D` but are missing the absolute position component.

### Manual Testing

Press **F5** during runtime to manually trigger a rebase for testing purposes.

## Configuration

### Rebase Threshold

Change the distance threshold that triggers a rebase:

```csharp
// Default: 1000 units
_floatingOriginManager = new FloatingOriginManager(World, _simulation, 5000.0);
```

### Debug Output

Uncomment the debug lines in the Update method to monitor the system:

```csharp
// Debug: Print floating origin info
if (_floatingOriginManager != null)
{
    var origin = _floatingOriginManager.CurrentOrigin;
    var camDist = new Vector3d(_camera.Position.X, _camera.Position.Y, _camera.Position.Z).Length();
    Console.WriteLine($"[FloatingOrigin] Current Origin: {origin}, Camera Distance: {camDist:F1}");
}
```

## Benefits

1. **Precision**: Maintains full precision for large-scale simulations
2. **Automatic**: No manual intervention required during gameplay
3. **Transparent**: Existing code continues to work with Position3D
4. **Physics Compatible**: Fully integrated with BepuPhysics simulation
5. **Performance**: Rebasing only occurs when necessary

## Technical Details

### Coordinate Systems

-   **Absolute Coordinates**: True world position using double precision
-   **Relative Coordinates**: Position relative to current floating origin using float precision
-   **Current Origin**: The offset that has been subtracted from all objects

### Rebase Process

1. Calculate new origin offset (usually camera position)
2. Update all entities with AbsolutePosition
3. Rebase physics bodies (kinematic/dynamic)
4. Rebase static physics objects (remove/re-add)
5. Update current origin tracking

### Thread Safety

The system is designed to run on the main thread during the Update loop. Rebasing operations are atomic and complete in a single frame.

## Example Scenarios

### Large Space Simulation

-   Planets at distances measured in AU (Astronomical Units)
-   Camera can travel millions of units from origin
-   System automatically maintains precision at all scales

### Seamless Travel

-   Player can travel from one planet to another
-   No "loading screens" or coordinate system changes
-   Physics simulation remains stable and accurate

## Troubleshooting

### Common Issues

1. **Missing AbsolutePosition**: Entities need both Position3D and AbsolutePosition
2. **Physics Desync**: Static objects may need manual rebase trigger
3. **Performance**: Very frequent rebasing may indicate threshold is too low

### Debug Commands

-   **F5**: Manual rebase trigger
-   **Console Output**: Monitor rebase operations and origin changes
-   **Entity Inspection**: Check AbsolutePosition vs Position3D values

## Future Enhancements

Potential improvements to consider:

1. **Hierarchical Origins**: Multiple origin systems for different scales
2. **Predictive Rebasing**: Anticipate need for rebase based on velocity
3. **Chunked Rebasing**: Rebase only nearby objects for performance
4. **Save/Load**: Persist absolute positions across game sessions
