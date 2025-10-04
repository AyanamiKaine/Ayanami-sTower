# Practical Example: Game AI with Tiered Interpreter

## Overview

This example demonstrates real-world scenarios where the **tiered interpreter** solves problems that are **difficult or impossible** with traditional switch-based interpreters.

**Use Case:** Tower Defense Game with Dynamic AI

## Why This Matters

Traditional game interpreters hard-code behaviors at compile time, requiring:

-   Full recompilation for any AI change
-   Server restarts (downtime for all players)
-   Separate test environments
-   Fixed difficulty levels
-   No community content support

The **tiered interpreter** enables:

-   ✅ Runtime plugin loading
-   ✅ Zero-downtime hotfixes
-   ✅ Live A/B testing
-   ✅ Dynamic difficulty adjustment
-   ✅ Community-created AI behaviors

## Scenarios Demonstrated

### 1. Plugin System - Community AI Mods

**Problem:** Players want to create custom AI behaviors, but traditional interpreters require recompilation.

**Solution:** Load community-created AI at runtime

```c
// Player creates "Speed Demon" AI that moves 2x faster
[PLUGIN] Loading community mod at runtime...
ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower_fast);
[PLUGIN] AI behavior replaced (automatic cache invalidation)
```

**Result:**

-   Hot-load plugins in milliseconds
-   No server restart
-   No player disconnects
-   Instant creativity unleashed

---

### 2. Live Debugging - Emergency Patches

**Problem:** During a tournament, a critical AI bug is discovered: enemies flee instead of attacking!

**Solution:** Hot-fix in production without downtime

```c
// Bug: Wrong handler registered
ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_flee);  // Oops!

// Emergency fix deployed in 5 seconds
[PATCH] Replacing AI_ATTACK_TOWER behavior...
ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_attack_tower);
[PATCH] Fix deployed! Match continues without restart.
```

**Result:**

-   Zero downtime
-   Players never disconnected
-   Tournament continues
-   Bug fixed in seconds (vs 30+ min restart)

---

### 3. A/B Testing - Optimize Player Experience

**Problem:** Game designer wants to test two AI strategies to see which is more fun.

**Solution:** Test both strategies in production with real players

```c
// Group A: Aggressive AI (direct attack)
ai_instruction_t aggressive_ai[] = {
    {AI_MOVE_TO_TOWER, 0},
    {AI_ATTACK_TOWER, 0},
    ...
};

// Group B: Tactical AI (circle, heal, then attack)
ai_instruction_t tactical_ai[] = {
    {AI_CHECK_HEALTH, 0},
    {AI_JUMP_IF_LOW_HEALTH, 50},
    {AI_CIRCLE_TOWER, 0},
    {AI_HEAL_SELF, 15},
    {AI_ATTACK_TOWER, 0},
    ...
};
```

**Result:**

-   Test in production with real players
-   Instant feedback on which is more fun
-   No separate test servers needed
-   Deploy winner immediately

---

### 4. Dynamic Difficulty - Adaptive Gameplay

**Problem:** Player died 3 times - game is too hard. But we don't want to force manual difficulty selection.

**Solution:** Automatically adjust AI difficulty based on player performance

```c
// Player struggling - auto-adjust
[SYSTEM] Player died 3 times. Reducing difficulty...

// Make enemies slower
ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower);  // Slower

// Disable reinforcements
ai_register_opcode(&interp, AI_CALL_REINFORCEMENTS, ai_skip_turn);

[ADJUST] Difficulty reduced seamlessly!
```

**Result:**

-   Player never notices the change
-   Game feels perfectly balanced
-   Personalized difficulty per player
-   No frustration, better retention

---

## Performance

Even with all this flexibility, the tiered system maintains excellent performance:

| Mode         | Performance      | Use Case                       |
| ------------ | ---------------- | ------------------------------ |
| **Cached**   | ~50-60M iter/sec | Stable behaviors (production)  |
| **Uncached** | ~1M iter/sec     | During modifications (testing) |
| **Speedup**  | ~50-60x          | Automatic switching            |

The system automatically:

-   Enters **uncached mode** when behaviors change (flexibility)
-   Returns to **cached mode** when stable (performance)
-   **Zero manual cache management** required

---

## Code Structure

### AI Opcodes

```c
typedef enum {
    /* Movement */
    AI_MOVE_TO_TOWER = 1,
    AI_MOVE_RANDOM = 2,
    AI_FLEE = 3,
    AI_CIRCLE_TOWER = 4,

    /* Combat */
    AI_ATTACK_TOWER = 10,
    AI_HEAL_SELF = 11,
    AI_CALL_REINFORCEMENTS = 12,

    /* Tactical */
    AI_CHECK_HEALTH = 20,
    AI_JUMP_IF_LOW_HEALTH = 21,
    AI_SKIP_TURN = 22,

    AI_HALT = 99
} ai_opcode_t;
```

### Game World

```c
typedef struct {
    int id, x, y;
    int health, damage;
    const char *type;
    bool alive;
} enemy_t;

typedef struct {
    enemy_t enemies[100];
    tower_t tower;
    int game_tick;
    int enemies_killed;
} game_world_t;
```

### AI VM

```c
typedef struct {
    int stack[32];
    int sp, pc;
    bool halted;

    game_world_t *world;  // Game context
    enemy_t *self;         // This enemy

    int moves_made;
    int attacks_made;
} ai_vm_t;
```

---

## Building and Running

```bash
# Build
cd build
cmake --build . --config Release --target sfpm_game_ai

# Run
./Release/sfpm_game_ai.exe
```

---

## Key Takeaways

### What Makes This Example "Practical"?

1. **Real Problem Space:** Tower defense games actually need:

    - Plugin systems for community content
    - Live debugging for production issues
    - A/B testing for game balance
    - Dynamic difficulty for player retention

2. **Impossible with Switch:** Traditional interpreters cannot:

    - Load behaviors at runtime
    - Change code without recompilation
    - Test strategies in production
    - Adapt to player performance

3. **Production-Ready:** The tiered system provides:
    - Zero-downtime deployments
    - Automatic cache management
    - High performance when stable
    - Full flexibility when needed

### Traditional vs Tiered Comparison

| Feature            | Traditional           | Tiered                    |
| ------------------ | --------------------- | ------------------------- |
| Plugin loading     | ❌ Requires recompile | ✅ Runtime hot-load       |
| Live patching      | ❌ Requires restart   | ✅ Zero downtime          |
| A/B testing        | ❌ Separate servers   | ✅ Production testing     |
| Dynamic difficulty | ❌ Fixed levels       | ✅ Adaptive per-player    |
| Community content  | ❌ Fork & rebuild     | ✅ Load plugins instantly |
| Performance        | ✅ Fast (native)      | ✅ Fast when stable       |
| Flexibility        | ❌ Compile-time only  | ✅ Runtime changes        |

---

## Extending the Example

### Add New AI Behaviors

```c
// Create custom behavior
static void ai_teleport_to_tower(ai_vm_t *vm, int operand) {
    vm->self->x = vm->world->tower.x;
    vm->self->y = vm->world->tower.y;
}

// Register at runtime
ai_register_opcode(&interp, AI_TELEPORT, ai_teleport_to_tower);
```

### Create Complex AI Scripts

```c
ai_instruction_t boss_ai[] = {
    {AI_CHECK_HEALTH, 0},
    {AI_JUMP_IF_LOW_HEALTH, 30},

    /* Phase 1: Aggressive */
    {AI_MOVE_TO_TOWER, 0},
    {AI_ATTACK_TOWER, 0},
    {AI_CALL_REINFORCEMENTS, 0},
    {AI_JUMP, 10},  // Skip phase 2

    /* Phase 2: Defensive (low health) */
    {AI_FLEE, 0},
    {AI_HEAL_SELF, 50},
    {AI_CIRCLE_TOWER, 0},

    {AI_HALT, 0}
};
```

### Implement Player Behavior Tracking

```c
// Track player performance
typedef struct {
    int deaths;
    int kills;
    int time_survived;
    float skill_rating;
} player_stats_t;

// Auto-adjust based on stats
void adjust_difficulty(ai_interpreter_t *interp, player_stats_t *stats) {
    if (stats->deaths > 3 && stats->skill_rating < 0.5) {
        // Make easier
        ai_register_opcode(interp, AI_MOVE_TO_TOWER, ai_move_to_tower);
        ai_register_opcode(interp, AI_ATTACK_TOWER, ai_attack_weak);
    } else if (stats->skill_rating > 0.8) {
        // Make harder
        ai_register_opcode(interp, AI_MOVE_TO_TOWER, ai_move_to_tower_fast);
        ai_register_opcode(interp, AI_ATTACK_TOWER, ai_attack_strong);
    }
}
```

---

## Real-World Applications

This pattern applies to:

1. **Game AI Systems**

    - Tower defense games
    - RTS unit behaviors
    - NPC AI in RPGs
    - Boss fight patterns

2. **Live Service Games**

    - Battle royale ring behaviors
    - Event-driven mechanics
    - Seasonal content updates
    - Emergency balance patches

3. **Mod Support**

    - Skyrim/Fallout script mods
    - Minecraft plugin systems
    - Warcraft 3 custom maps
    - Roblox user experiences

4. **Cloud Gaming**
    - Server-side AI updates
    - No client downloads
    - Instant content delivery
    - Per-region customization

---

## Conclusion

The **tiered interpreter** transforms game development from:

❌ **Compile → Deploy → Pray**

To:

✅ **Test → Iterate → Deploy → Adjust → Succeed**

**Key Benefits:**

-   Faster development cycles
-   Lower risk deployments
-   Better player experience
-   Community-driven content
-   Data-driven design decisions

All while maintaining **near-native performance** when behaviors are stable!

---

## See Also

-   `interpreter_tiered.c` - Core tiered system implementation
-   `interpreter_comparison.c` - Performance comparison
-   `interpreter_cached.c` - Caching strategies
-   `README_TIERED.md` - Tiered system documentation
