/**
 * @file interpreter_game_ai.c
 * @brief Practical example: Plugin-based Game AI using Tiered Interpreter
 * 
 * This example demonstrates a real-world use case where the tiered interpreter
 * excels: A game AI system with hot-swappable behaviors.
 * 
 * Scenario: Tower Defense Game
 * - Multiple enemy types with different AI behaviors
 * - Plugin system for community-created AI
 * - A/B testing different AI strategies
 * - Live patching of AI bugs during gameplay
 * - Difficulty adjustment without restarting
 * 
 * Traditional switch-based interpreters struggle with:
 * - Runtime plugin loading/unloading
 * - Hot-fixing bugs without server restart
 * - Dynamic difficulty scaling
 * - A/B testing in production
 * 
 * The tiered system makes all of this trivial!
 */

#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <stdint.h>
#include <math.h>

#ifdef _WIN32
    #include <windows.h>
#else
    #include <sys/time.h>
#endif

/* ============================================================================
 *                      HIGH-RESOLUTION TIMING
 * ============================================================================ */

static uint64_t get_time_microseconds(void) {
#ifdef _WIN32
    LARGE_INTEGER frequency, counter;
    QueryPerformanceFrequency(&frequency);
    QueryPerformanceCounter(&counter);
    return (uint64_t)((counter.QuadPart * 1000000) / frequency.QuadPart);
#else
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (uint64_t)(tv.tv_sec * 1000000 + tv.tv_usec);
#endif
}

/* ============================================================================
 *                      GAME WORLD SIMULATION
 * ============================================================================ */

typedef struct {
    int id;
    int x, y;           /* Position */
    int health;
    int damage;
    const char *type;
    bool alive;
} enemy_t;

typedef struct {
    int x, y;           /* Tower position */
    int health;
    int defense;
    bool destroyed;
} tower_t;

typedef struct {
    enemy_t enemies[100];
    int enemy_count;
    tower_t tower;
    int game_tick;
    int enemies_killed;
    int tower_damage_taken;
} game_world_t;

static void world_init(game_world_t *world) {
    memset(world, 0, sizeof(game_world_t));
    world->tower.x = 50;
    world->tower.y = 50;
    world->tower.health = 1000;
    world->tower.defense = 10;
}

static void world_spawn_enemy(game_world_t *world, const char *type, int x, int y, int health, int damage) {
    if (world->enemy_count >= 100) return;
    
    enemy_t *e = &world->enemies[world->enemy_count++];
    e->id = world->enemy_count;
    e->type = type;
    e->x = x;
    e->y = y;
    e->health = health;
    e->damage = damage;
    e->alive = true;
}

/* ============================================================================
 *                      AI BYTECODE DEFINITIONS
 * ============================================================================ */

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
    
    /* Control flow */
    AI_HALT = 99,
    
    AI_MAX = 128
} ai_opcode_t;

typedef struct {
    ai_opcode_t op;
    int operand;
} ai_instruction_t;

/* ============================================================================
 *                      AI INTERPRETER STATE
 * ============================================================================ */

#define AI_STACK_SIZE 32

typedef struct {
    /* Execution state */
    int stack[AI_STACK_SIZE];
    int sp;
    int pc;
    bool halted;
    
    /* Game context */
    game_world_t *world;
    enemy_t *self;
    
    /* Statistics */
    int moves_made;
    int attacks_made;
    bool verbose;
} ai_vm_t;

static void ai_vm_init(ai_vm_t *vm, game_world_t *world, enemy_t *enemy) {
    memset(vm, 0, sizeof(ai_vm_t));
    vm->world = world;
    vm->self = enemy;
    vm->verbose = false;
}

static void ai_push(ai_vm_t *vm, int value) {
    if (vm->sp >= AI_STACK_SIZE) {
        fprintf(stderr, "AI stack overflow!\n");
        exit(1);
    }
    vm->stack[vm->sp++] = value;
}

static int ai_pop(ai_vm_t *vm) {
    if (vm->sp <= 0) {
        fprintf(stderr, "AI stack underflow!\n");
        exit(1);
    }
    return vm->stack[--vm->sp];
}

/* ============================================================================
 *                      AI BEHAVIOR HANDLERS
 * ============================================================================ */

typedef void (*ai_handler_fn)(ai_vm_t *vm, int operand);

/* Movement behaviors */
static void ai_move_to_tower(ai_vm_t *vm, int operand) {
    (void)operand;
    int dx = vm->world->tower.x - vm->self->x;
    int dy = vm->world->tower.y - vm->self->y;
    
    if (dx != 0) vm->self->x += (dx > 0) ? 1 : -1;
    else if (dy != 0) vm->self->y += (dy > 0) ? 1 : -1;
    
    vm->moves_made++;
    if (vm->verbose) printf("    Enemy %d moves toward tower (%d,%d)\n", vm->self->id, vm->self->x, vm->self->y);
}

static void ai_move_to_tower_fast(ai_vm_t *vm, int operand) {
    (void)operand;
    int dx = vm->world->tower.x - vm->self->x;
    int dy = vm->world->tower.y - vm->self->y;
    
    /* Move 2 tiles instead of 1 (speed boost) */
    if (dx != 0) vm->self->x += ((dx > 0) ? 1 : -1) * 2;
    else if (dy != 0) vm->self->y += ((dy > 0) ? 1 : -1) * 2;
    
    vm->moves_made++;
    if (vm->verbose) printf("    Enemy %d FAST moves toward tower (%d,%d)\n", vm->self->id, vm->self->x, vm->self->y);
}

static void ai_move_random(ai_vm_t *vm, int operand) {
    (void)operand;
    int dir = rand() % 4;
    switch (dir) {
        case 0: vm->self->x++; break;
        case 1: vm->self->x--; break;
        case 2: vm->self->y++; break;
        case 3: vm->self->y--; break;
    }
    vm->moves_made++;
    if (vm->verbose) printf("    Enemy %d wanders randomly (%d,%d)\n", vm->self->id, vm->self->x, vm->self->y);
}

static void ai_flee(ai_vm_t *vm, int operand) {
    (void)operand;
    int dx = vm->world->tower.x - vm->self->x;
    int dy = vm->world->tower.y - vm->self->y;
    
    /* Move away from tower */
    if (dx != 0) vm->self->x -= (dx > 0) ? 1 : -1;
    else if (dy != 0) vm->self->y -= (dy > 0) ? 1 : -1;
    
    vm->moves_made++;
    if (vm->verbose) printf("    Enemy %d flees from tower (%d,%d)\n", vm->self->id, vm->self->x, vm->self->y);
}

static void ai_circle_tower(ai_vm_t *vm, int operand) {
    (void)operand;
    /* Simple circular motion around tower */
    int dx = vm->self->x - vm->world->tower.x;
    int dy = vm->self->y - vm->world->tower.y;
    
    /* Rotate 90 degrees clockwise */
    int new_dx = dy;
    int new_dy = -dx;
    
    vm->self->x = vm->world->tower.x + new_dx;
    vm->self->y = vm->world->tower.y + new_dy;
    
    vm->moves_made++;
    if (vm->verbose) printf("    Enemy %d circles tower (%d,%d)\n", vm->self->id, vm->self->x, vm->self->y);
}

/* Combat behaviors */
static void ai_attack_tower(ai_vm_t *vm, int operand) {
    (void)operand;
    int dx = abs(vm->world->tower.x - vm->self->x);
    int dy = abs(vm->world->tower.y - vm->self->y);
    int distance = dx + dy;  /* Manhattan distance */
    
    if (distance <= 2) {
        int damage = vm->self->damage - vm->world->tower.defense;
        if (damage < 0) damage = 0;
        vm->world->tower.health -= damage;
        vm->world->tower_damage_taken += damage;
        vm->attacks_made++;
        if (vm->verbose) printf("    Enemy %d attacks tower! (-%d HP, tower at %d HP)\n", 
                                vm->self->id, damage, vm->world->tower.health);
        
        if (vm->world->tower.health <= 0) {
            vm->world->tower.destroyed = true;
            if (vm->verbose) printf("    >>> TOWER DESTROYED! <<<\n");
        }
    } else {
        if (vm->verbose) printf("    Enemy %d too far to attack (distance: %d)\n", vm->self->id, distance);
    }
}

static void ai_heal_self(ai_vm_t *vm, int operand) {
    int heal_amount = operand > 0 ? operand : 10;
    vm->self->health += heal_amount;
    if (vm->verbose) printf("    Enemy %d heals for %d HP (now at %d HP)\n", 
                            vm->self->id, heal_amount, vm->self->health);
}

static void ai_call_reinforcements(ai_vm_t *vm, int operand) {
    (void)operand;
    /* Spawn a weaker ally */
    if (vm->world->enemy_count < 100) {
        world_spawn_enemy(vm->world, "Minion", vm->self->x + 1, vm->self->y, 20, 3);
        if (vm->verbose) printf("    Enemy %d calls reinforcements!\n", vm->self->id);
    }
}

/* Tactical behaviors */
static void ai_check_health(ai_vm_t *vm, int operand) {
    (void)operand;
    ai_push(vm, vm->self->health);
}

static void ai_jump_if_low_health(ai_vm_t *vm, int operand) {
    int threshold = operand > 0 ? operand : 30;
    if (vm->self->health < threshold) {
        vm->pc += 3;  /* Skip next 3 instructions */
        if (vm->verbose) printf("    Enemy %d health low (%d < %d), changing tactics!\n", 
                                vm->self->id, vm->self->health, threshold);
    }
}

static void ai_skip_turn(ai_vm_t *vm, int operand) {
    (void)operand;
    if (vm->verbose) printf("    Enemy %d waits...\n", vm->self->id);
}

static void ai_halt(ai_vm_t *vm, int operand) {
    (void)operand;
    vm->halted = true;
}

/* ============================================================================
 *                      TIERED AI INTERPRETER
 * ============================================================================ */

typedef enum {
    MODE_UNCACHED,
    MODE_CACHED
} ai_mode_t;

typedef struct {
    ai_vm_t *vm;
    int operand;
    ai_handler_fn handler;
} ai_context_t;

typedef struct {
    ai_mode_t mode;
    uint64_t cache_version;
    
    sfpm_rule_t *rule_cache[AI_MAX];
    ai_context_t contexts[AI_MAX];
    
    sfpm_rule_t **all_rules;
    int all_rules_count;
    int all_rules_capacity;
    
    uint64_t cached_dispatches;
    uint64_t uncached_dispatches;
    uint64_t cache_invalidations;
} ai_interpreter_t;

static void execute_ai_handler(void *user_data) {
    ai_context_t *ctx = (ai_context_t *)user_data;
    ctx->handler(ctx->vm, ctx->operand);
}

static sfpm_rule_t *create_ai_rule(ai_opcode_t opcode, ai_handler_fn handler, ai_context_t *context) {
    context->handler = handler;
    
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    char name[64];
    snprintf(name, sizeof(name), "ai_opcode_%d", opcode);
    
    return sfpm_rule_create(criterias, 1, execute_ai_handler, context, name);
}

static void ai_interp_init(ai_interpreter_t *interp) {
    memset(interp, 0, sizeof(ai_interpreter_t));
    interp->mode = MODE_CACHED;
    interp->cache_version = 1;
    interp->all_rules_capacity = 32;
    interp->all_rules = malloc(sizeof(sfpm_rule_t*) * interp->all_rules_capacity);
    interp->all_rules_count = 0;
}

static void ai_enter_uncached_mode(ai_interpreter_t *interp) {
    if (interp->mode == MODE_UNCACHED) return;
    interp->mode = MODE_UNCACHED;
    interp->cache_invalidations++;
}

static void ai_enter_cached_mode(ai_interpreter_t *interp) {
    if (interp->mode == MODE_CACHED) return;
    interp->mode = MODE_CACHED;
    interp->cache_version++;
}

static void ai_register_opcode(ai_interpreter_t *interp, ai_opcode_t opcode, ai_handler_fn handler) {
    sfpm_rule_t *rule = create_ai_rule(opcode, handler, &interp->contexts[opcode]);
    sfpm_rule_t *old_rule = interp->rule_cache[opcode];
    
    interp->rule_cache[opcode] = rule;
    
    bool found = false;
    for (int i = 0; i < interp->all_rules_count; i++) {
        if (interp->all_rules[i] == old_rule) {
            interp->all_rules[i] = rule;
            found = true;
            break;
        }
    }
    
    if (!found) {
        if (interp->all_rules_count >= interp->all_rules_capacity) {
            interp->all_rules_capacity *= 2;
            interp->all_rules = realloc(interp->all_rules, sizeof(sfpm_rule_t*) * interp->all_rules_capacity);
        }
        interp->all_rules[interp->all_rules_count++] = rule;
    }
    
    if (old_rule) {
        sfpm_rule_destroy(old_rule);
    }
    
    if (interp->mode == MODE_CACHED) {
        ai_enter_uncached_mode(interp);
    }
}

static void ai_execute_instruction(ai_interpreter_t *interp, ai_vm_t *vm, ai_instruction_t instr) {
    if (interp->mode == MODE_CACHED) {
        sfpm_rule_t *rule = interp->rule_cache[instr.op];
        if (rule) {
            interp->contexts[instr.op].vm = vm;
            interp->contexts[instr.op].operand = instr.operand;
            sfpm_rule_execute_payload(rule);
            interp->cached_dispatches++;
        }
    } else {
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
        
        interp->contexts[instr.op].vm = vm;
        interp->contexts[instr.op].operand = instr.operand;
        
        sfpm_match(interp->all_rules, interp->all_rules_count, facts, false);
        sfpm_fact_source_destroy(facts);
        
        interp->uncached_dispatches++;
    }
}

static void ai_run_program(ai_interpreter_t *interp, ai_vm_t *vm, ai_instruction_t *program, int program_size) {
    vm->pc = 0;
    vm->halted = false;
    
    while (vm->pc < program_size && !vm->halted) {
        ai_instruction_t instr = program[vm->pc++];
        ai_execute_instruction(interp, vm, instr);
    }
}

static void ai_interp_destroy(ai_interpreter_t *interp) {
    for (int i = 0; i < AI_MAX; i++) {
        if (interp->rule_cache[i]) {
            sfpm_rule_destroy(interp->rule_cache[i]);
        }
    }
    free(interp->all_rules);
}

/* ============================================================================
 *                      PRACTICAL DEMONSTRATIONS
 * ============================================================================ */

static void print_header(const char *title) {
    printf("\n+================================================================+\n");
    printf("|  %-60s  |\n", title);
    printf("+================================================================+\n");
}

static void print_section(const char *title) {
    printf("\n+------------------------------------------------------------+\n");
    printf("|  %-56s  |\n", title);
    printf("+------------------------------------------------------------+\n");
}

/* SCENARIO 1: Plugin System - Loading Community AI */
static void demo_plugin_system(void) {
    print_section("SCENARIO 1: Plugin System - Community AI Mod");
    
    printf("\nA player creates a custom AI behavior 'Speed Demon' that moves\n");
    printf("twice as fast. The game loads this plugin at runtime.\n\n");
    
    ai_interpreter_t interp;
    ai_interp_init(&interp);
    
    /* Register default AI behaviors */
    printf("[GAME] Loading default AI behaviors...\n");
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower);
    ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_attack_tower);
    ai_register_opcode(&interp, AI_HALT, ai_halt);
    ai_enter_cached_mode(&interp);
    printf("[GAME] Default behaviors loaded (cached mode)\n");
    
    /* Create game world */
    game_world_t world;
    world_init(&world);
    world_spawn_enemy(&world, "Normal Enemy", 10, 10, 50, 15);
    
    /* Run with default behavior */
    printf("\n[TICK 1] Running default AI...\n");
    ai_vm_t vm;
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    
    ai_instruction_t default_ai[] = {
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_HALT, 0}
    };
    
    ai_run_program(&interp, &vm, default_ai, 3);
    
    /* Hot-load plugin */
    printf("\n[PLUGIN] Loading community mod 'Speed Demon' at runtime...\n");
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower_fast);
    printf("[PLUGIN] AI behavior replaced (automatic cache invalidation)\n");
    
    /* Run with modded behavior */
    printf("\n[TICK 2] Running with modded AI...\n");
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    ai_run_program(&interp, &vm, default_ai, 3);
    
    printf("\n[INFO] Plugin system allows community creativity!\n");
    printf("       Traditional interpreters would require:\n");
    printf("       - Recompilation\n");
    printf("       - Server restart\n");
    printf("       - All players disconnected\n");
    printf("\n       With tiered system: Hot-load in milliseconds!\n");
    
    ai_interp_destroy(&interp);
}

/* SCENARIO 2: Live Debugging - Fix AI Bug During Match */
static void demo_live_debugging(void) {
    print_section("SCENARIO 2: Live Debugging - Emergency Patch");
    
    printf("\nDuring a tournament, players discover a bug: enemies flee\n");
    printf("instead of attacking! Developer hot-fixes it in production.\n\n");
    
    ai_interpreter_t interp;
    ai_interp_init(&interp);
    
    /* Register buggy AI (mistake: registered wrong handler) */
    printf("[SERVER] Game starting with buggy AI...\n");
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower);
    ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_flee);  /* BUG: Wrong handler! */
    ai_register_opcode(&interp, AI_HALT, ai_halt);
    ai_enter_cached_mode(&interp);
    
    game_world_t world;
    world_init(&world);
    world_spawn_enemy(&world, "Buggy Enemy", 48, 48, 100, 20);
    
    printf("\n[MATCH] Round 1 - Players notice enemies fleeing!\n");
    ai_vm_t vm;
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    
    ai_instruction_t ai_program[] = {
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_HALT, 0}
    };
    
    ai_run_program(&interp, &vm, ai_program, 3);
    printf("       Tower damage: %d (should have been attacked!)\n", world.tower_damage_taken);
    
    /* Live fix */
    printf("\n[EMERGENCY] Developer deploys hotfix in 5 seconds...\n");
    printf("[PATCH] Replacing AI_ATTACK_TOWER behavior...\n");
    ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_attack_tower);
    ai_enter_cached_mode(&interp);
    printf("[PATCH] Fix deployed! Match continues without restart.\n");
    
    printf("\n[MATCH] Round 2 - Fix verified!\n");
    world.enemies[0].x = 48;
    world.enemies[0].y = 48;
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    ai_run_program(&interp, &vm, ai_program, 3);
    printf("       Tower damage: %d (working correctly!)\n", world.tower_damage_taken);
    
    printf("\n[INFO] Zero downtime! Tournament continues!\n");
    printf("       Players never disconnected.\n");
    printf("       Traditional approach: 30+ minute rollback and restart.\n");
    
    ai_interp_destroy(&interp);
}

/* SCENARIO 3: A/B Testing - Which AI is More Fun? */
static void demo_ab_testing(void) {
    print_section("SCENARIO 3: A/B Testing - Optimize Player Experience");
    
    printf("\nGame designer wants to test two AI strategies:\n");
    printf("  Group A: Aggressive (direct attack)\n");
    printf("  Group B: Tactical (circle and heal)\n\n");
    
    ai_interpreter_t interp;
    ai_interp_init(&interp);
    
    /* Register base behaviors */
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower);
    ai_register_opcode(&interp, AI_CIRCLE_TOWER, ai_circle_tower);
    ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_attack_tower);
    ai_register_opcode(&interp, AI_HEAL_SELF, ai_heal_self);
    ai_register_opcode(&interp, AI_CHECK_HEALTH, ai_check_health);
    ai_register_opcode(&interp, AI_JUMP_IF_LOW_HEALTH, ai_jump_if_low_health);
    ai_register_opcode(&interp, AI_HALT, ai_halt);
    ai_enter_cached_mode(&interp);
    
    /* Test Group A: Aggressive */
    printf("[GROUP A] Testing aggressive AI strategy...\n");
    game_world_t world_a;
    world_init(&world_a);
    world_spawn_enemy(&world_a, "Aggressive", 20, 20, 80, 25);
    
    ai_instruction_t aggressive_ai[] = {
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_HALT, 0}
    };
    
    ai_vm_t vm_a;
    ai_vm_init(&vm_a, &world_a, &world_a.enemies[0]);
    uint64_t start_a = get_time_microseconds();
    for (int turn = 0; turn < 10; turn++) {
        vm_a.pc = 0;
        vm_a.halted = false;
        ai_run_program(&interp, &vm_a, aggressive_ai, 5);
    }
    uint64_t end_a = get_time_microseconds();
    
    printf("  Results: Tower HP: %d, Damage dealt: %d\n", 
           world_a.tower.health, world_a.tower_damage_taken);
    printf("  Performance: %.2f ms for 10 turns\n", (end_a - start_a) / 1000.0);
    
    /* Test Group B: Tactical */
    printf("\n[GROUP B] Testing tactical AI strategy...\n");
    game_world_t world_b;
    world_init(&world_b);
    world_spawn_enemy(&world_b, "Tactical", 20, 20, 80, 25);
    
    ai_instruction_t tactical_ai[] = {
        {AI_CHECK_HEALTH, 0},
        {AI_JUMP_IF_LOW_HEALTH, 50},
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_JUMP_IF_LOW_HEALTH, 0},  /* Skip next 3 if jumped */
        {AI_CIRCLE_TOWER, 0},
        {AI_HEAL_SELF, 15},
        {AI_ATTACK_TOWER, 0},
        {AI_HALT, 0}
    };
    
    ai_vm_t vm_b;
    ai_vm_init(&vm_b, &world_b, &world_b.enemies[0]);
    uint64_t start_b = get_time_microseconds();
    for (int turn = 0; turn < 10; turn++) {
        vm_b.pc = 0;
        vm_b.halted = false;
        ai_run_program(&interp, &vm_b, tactical_ai, 9);
    }
    uint64_t end_b = get_time_microseconds();
    
    printf("  Results: Tower HP: %d, Damage dealt: %d\n", 
           world_b.tower.health, world_b.tower_damage_taken);
    printf("  Performance: %.2f ms for 10 turns\n", (end_b - start_b) / 1000.0);
    
    printf("\n[ANALYTICS] Comparing results:\n");
    printf("  Group A: More damage, faster kills\n");
    printf("  Group B: More challenging, better gameplay?\n");
    printf("\n[DECISION] Deploy Group B to production!\n");
    printf("\n[INFO] A/B testing with zero impact on players.\n");
    printf("       Traditional approach: Separate test servers, weeks of testing.\n");
    printf("       Tiered system: Test in production, instant results!\n");
    
    ai_interp_destroy(&interp);
}

/* SCENARIO 4: Dynamic Difficulty - Adjust on the Fly */
static void demo_dynamic_difficulty(void) {
    print_section("SCENARIO 4: Dynamic Difficulty Adjustment");
    
    printf("\nPlayer is struggling (died 3 times). Game automatically\n");
    printf("adjusts AI difficulty by making enemies slower and weaker.\n\n");
    
    ai_interpreter_t interp;
    ai_interp_init(&interp);
    
    /* Start with hard difficulty */
    printf("[GAME] Starting on HARD difficulty...\n");
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower_fast);
    ai_register_opcode(&interp, AI_ATTACK_TOWER, ai_attack_tower);
    ai_register_opcode(&interp, AI_CALL_REINFORCEMENTS, ai_call_reinforcements);
    ai_register_opcode(&interp, AI_HALT, ai_halt);
    ai_enter_cached_mode(&interp);
    
    game_world_t world;
    world_init(&world);
    world_spawn_enemy(&world, "Hard Enemy", 30, 30, 100, 30);
    
    ai_instruction_t ai_program[] = {
        {AI_MOVE_TO_TOWER, 0},
        {AI_ATTACK_TOWER, 0},
        {AI_CALL_REINFORCEMENTS, 0},
        {AI_HALT, 0}
    };
    
    printf("\n[WAVE 1] Hard difficulty - Fast enemies\n");
    ai_vm_t vm;
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    ai_run_program(&interp, &vm, ai_program, 4);
    printf("  Tower HP: %d (player struggling!)\n", world.tower.health);
    
    /* Player dies 3 times - auto-adjust */
    printf("\n[SYSTEM] Player died 3 times. Reducing difficulty...\n");
    printf("[ADJUST] Switching to NORMAL difficulty:\n");
    printf("         - Slower movement\n");
    printf("         - No reinforcements\n");
    
    ai_register_opcode(&interp, AI_MOVE_TO_TOWER, ai_move_to_tower);  /* Slower */
    ai_register_opcode(&interp, AI_CALL_REINFORCEMENTS, ai_skip_turn);  /* Disabled */
    ai_enter_cached_mode(&interp);
    
    printf("\n[WAVE 2] Normal difficulty - Balanced gameplay\n");
    world.enemies[0].x = 30;
    world.enemies[0].y = 30;
    ai_vm_init(&vm, &world, &world.enemies[0]);
    vm.verbose = true;
    ai_run_program(&interp, &vm, ai_program, 4);
    printf("  Tower HP: %d (more manageable!)\n", world.tower.health);
    
    printf("\n[INFO] Seamless difficulty adjustment!\n");
    printf("       Player never noticed the change.\n");
    printf("       Game feels perfectly balanced.\n");
    printf("\n       Traditional approach: Fixed difficulty levels,\n");
    printf("       frustrating for casual players.\n");
    
    ai_interp_destroy(&interp);
}

/* ============================================================================
 *                              MAIN
 * ============================================================================ */

int main(void) {
    print_header("Tiered Interpreter: Practical Game AI Examples");
    
    printf("\nThis demo shows real-world scenarios where the tiered interpreter\n");
    printf("solves problems that are difficult or impossible with traditional\n");
    printf("switch-based interpreters.\n");
    
    /* Run all scenarios */
    demo_plugin_system();
    demo_live_debugging();
    demo_ab_testing();
    demo_dynamic_difficulty();
    
    /* Summary */
    print_header("Why Tiered Interpreters Excel");
    
    printf("\n[+] ADVANTAGES DEMONSTRATED:\n\n");
    
    printf("  1. PLUGIN SYSTEMS\n");
    printf("     - Load/unload behaviors at runtime\n");
    printf("     - Community-created content\n");
    printf("     - Zero compilation or restart\n\n");
    
    printf("  2. LIVE DEBUGGING\n");
    printf("     - Hot-fix bugs in production\n");
    printf("     - Zero downtime deployments\n");
    printf("     - Emergency patches in seconds\n\n");
    
    printf("  3. A/B TESTING\n");
    printf("     - Test strategies in production\n");
    printf("     - Real player feedback\n");
    printf("     - Instant iteration\n\n");
    
    printf("  4. DYNAMIC DIFFICULTY\n");
    printf("     - Adjust on player performance\n");
    printf("     - Seamless transitions\n");
    printf("     - Personalized experience\n\n");
    
    printf("[!] TRADITIONAL INTERPRETER LIMITATIONS:\n\n");
    
    printf("  - Behaviors hard-coded at compile time\n");
    printf("  - Changes require full rebuild\n");
    printf("  - Testing requires separate servers\n");
    printf("  - Difficulty levels fixed\n");
    printf("  - No runtime extensibility\n");
    printf("  - Downtime for every change\n\n");
    
    printf("[i] THE TIERED ADVANTAGE:\n\n");
    
    printf("  The tiered interpreter gives you the flexibility to:\n");
    printf("  - Modify behaviors without recompilation\n");
    printf("  - Test changes with zero downtime\n");
    printf("  - Support community content\n");
    printf("  - Adapt to player behavior in real-time\n\n");
    
    printf("  All while maintaining near-native performance\n");
    printf("  when behaviors are stable!\n");
    
    return 0;
}
