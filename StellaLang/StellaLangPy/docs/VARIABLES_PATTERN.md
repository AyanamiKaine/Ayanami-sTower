# Using VM Variables Instead of Direct Attributes

## Overview

In VMActor, we store game state in `vm.variables` instead of as direct attributes on the VM object (like `vm.enemies`). This approach is more aligned with the actor model and enables better message-based interactions.

## Benefits

### 1. **Actor-Oriented Design**

Variables are stored in a dedicated namespace (`vm.variables`) that clearly represents the actor's state.

```python
# Before (direct attributes)
vm.enemies = []
vm.player = GameObject(...)
vm.score = 0

# After (variables)
vm.variables['enemies'] = []
vm.variables['player'] = GameObject(...)
vm.variables['score'] = 0
```

### 2. **Message-Based Access**

Other actors can interact with state through messages without directly accessing VM internals.

```python
# Functions access state through variables
def update_player(vm):
    player = vm.variables['player']
    enemies = vm.variables['enemies']
    # ... game logic
```

### 3. **Clear Separation of Concerns**

- **VM Infrastructure**: `vm.stack`, `vm.ip`, `vm.bytecode`, `vm.instruction_table`
- **Application State**: `vm.variables['player']`, `vm.variables['enemies']`, etc.

### 4. **Enables Future Message Protocols**

With variables as the state container, you can easily add:

- Get variable messages: `(get 'player)`
- Set variable messages: `(set 'score 100)`
- Query messages: `(get-all-variables)`

## Example: Current Implementation

In `game_hot_reload.py`, the refactored code now uses variables consistently:

```python
class GameActor(VMActor):
    def __init__(self, name):
        super().__init__()
        self.name = name

        # Store in variables instead of direct attributes
        self.variables['player'] = GameObject(WIDTH // 2, HEIGHT // 2, 20, BLUE)
        self.variables['enemies'] = []
        self.variables['particles'] = []
        self.variables['score'] = 0
        self.variables['wave'] = 1
        self.variables['paused'] = False
```

Functions access state through variables:

```python
def update_player(vm):
    keys = pygame.key.get_pressed()
    player = vm.variables['player']  # Get from variables

    # ... modify player state
    player.x += player.vx
    player.y += player.vy
```

## Future Enhancements

With this pattern, you can easily implement:

### Inter-Actor Communication

```python
# Actor A asks Actor B for game state
def get_enemy_count(vm):
    enemies = vm.variables['enemies']
    return len(enemies)

# Another actor queries this via message
bytecode = game_actor.s_expression_to_bytecode('(get-enemy-count)')
game_actor.send(*bytecode)
```

### State Inspection

```python
# Debugging or monitoring actors can query state
def inspect_state(vm):
    return {
        'player_pos': (vm.variables['player'].x, vm.variables['player'].y),
        'enemy_count': len(vm.variables['enemies']),
        'score': vm.variables['score']
    }
```

### Variable Watchers

```python
# Future feature: Watch for changes to specific variables
vm.watch_variable('score', on_score_change)
```

## Pattern Summary

**DO:**

- ✅ Store application state in `vm.variables`
- ✅ Access state through `vm.variables['key']`
- ✅ Keep VM infrastructure separate from app state

**DON'T:**

- ❌ Store application state as direct VM attributes
- ❌ Mix VM internals with game state
- ❌ Access state without going through variables

This pattern keeps your actor system clean, message-oriented, and ready for future enhancements!
