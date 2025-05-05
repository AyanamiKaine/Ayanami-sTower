# NihilEx.Memory - Flexible In-Memory Data Store

## Overview

`NihilEx.Memory` provides a type-safe, generic, and thread-safe in-memory storage mechanism designed for .NET applications, particularly useful in game development scenarios (like Unity with C# or other game engines).

The core idea, inspired by the need to attach arbitrary metadata to game entities without cluttering their core classes or components, is to provide a flexible "memory" object. This object can hold various pieces of information (facts) as key-value pairs, where the types of keys and values can differ across different collections within the same `Memory` instance.

**Key Use Cases:**

- Storing temporary or situational data for characters (player, NPCs), game regions, or other entities (e.g., observations, dialogue states, quest flags, temporary statuses).
- Acting as a fact source for rule engines or decision-making systems, like the **[AyanamisTower.SFPM (Stella Fuzzy Pattern Matcher)](https://github.com/AyanamiKaine/Ayanami-sTower/tree/main/stella_fuzzy_pattern_matcher)**.
- Avoiding the need to add numerous specific fields to entity classes for data that isn't core to their identity or state, especially data not accessed every frame.

**Thread Safety:** The `Memory` class uses `System.Collections.Concurrent.ConcurrentDictionary` internally for storing its type-specific dictionaries and also for the inner dictionaries holding the actual key-value facts. This ensures that operations like adding, removing, updating, and retrieving facts (`SetValue`, `RemoveValue`, `TryGetValue`, etc.) are thread-safe.

## Features

- **Type-Safe Storage:** Stores data in distinct collections based on `TKey`/`TValue` types (e.g., `string`/`int` facts are separate from `Guid`/`bool` facts).
- **Flexible:** Store almost any kind of data using generic keys and values.
- **Thread-Safe:** Operations on facts (add, update, remove, read) are thread-safe.
- **Fluent API:** `SetValue` returns the `Memory` instance for chaining.
- **Indexer Access:** Provides convenient indexer-style access via the `For<TKey, TValue>()` helper struct.
- **SFPM Integration:** Implements the `AyanamisTower.SFPM.IFactSource` interface, allowing seamless use as a fact provider for the Stella Fuzzy Pattern Matcher.

## Basic Usage

```csharp
using AyanamisTower.NihilEx;
using System;


public static class MemoryKeys
{
    public const string ObservedSuspiciousActivity = "ObservedSuspiciousActivity";
    public const string TimesVisitedTavern = "TimesVisitedTavern";
    public const string PlayerSentiment = "PlayerSentiment";
    public const string QuestFindLostAmulet_State = "QuestFindLostAmuletState";
    public const string RegionAltarActivated = "RegionAltarActivated";
    public const string DialogueLearnedSecretWeakness = "DialogueLearnedSecretWeakness";
}

// Create a memory instance (e.g., for a player)
var playerMemory = new Memory();

// --- Using SetValue / GetValue / TryGetValue ---

// Set some facts of different types
playerMemory.SetValue("PlayerName", "Alice"); // string/string
playerMemory.SetValue("Level", 15);           // string/int
playerMemory.SetValue("IsStealthed", false);  // string/bool
playerMemory.SetValue(Guid.NewGuid(), DateTime.Now); // Guid/DateTime

// Get values (GetValue throws KeyNotFoundException if key/type doesn't exist)
int level = playerMemory.GetValue<string, int>("Level");
Console.WriteLine($"Player Level: {level}"); // Output: Player Level: 15

// Try get values (safer, returns bool)
if (playerMemory.TryGetValue<string, bool>("IsStealthed", out bool isStealthed))
{
    Console.WriteLine($"Is Stealthed: {isStealthed}"); // Output: Is Stealthed: False
}

if (!playerMemory.TryGetValue<string, int>("Score", out int score))
{
    Console.WriteLine("Score fact not found."); // Output: Score fact not found.
}

// --- Using the For<TKey, TValue>() indexer ---

// Set value using indexer
playerMemory.For<string, int>()["Level"] = 16;

// Get value using indexer
int updatedLevel = playerMemory.For<string, int>()["Level"];
Console.WriteLine($"Updated Player Level: {updatedLevel}"); // Output: Updated Player Level: 16

// Check keys using indexer helper methods (if added in TypedMemoryAccessor)
bool hasLevel = playerMemory.For<string, int>().ContainsKey("Level"); // True
Console.WriteLine($"Has Level Key: {hasLevel}");

// --- Removing and Clearing ---

bool removed = playerMemory.RemoveValue<string, bool>("IsStealthed");
Console.WriteLine($"Removed 'IsStealthed': {removed}"); // Output: Removed 'IsStealthed': True

// Clear all string/int facts
playerMemory.Clear<string, int>();
Console.WriteLine($"Has Level Key after Clear: {playerMemory.For<string, int>().ContainsKey("Level")}"); // Output: False

// Clear everything
playerMemory.ClearAll();
```

## Stella Fuzzy Pattern Matcher (SFPM) Integration

The `Memory` class implements `AyanamisTower.SFPM.IFactSource`, allowing it to be directly used as the source of facts for SFPM rules.

```csharp
using AyanamisTower.NihilEx;
using AyanamisTower.SFPM;
using System;
using System.Collections.Generic;

// Assume MemoryKeys and Sentiment enum are defined as in previous examples

// --- Setup ---
var npcMemory = new Memory();
var rules = new List<Rule>();
string dialogueOutput = "NPC: Hello there."; // Default dialogue

// Populate NPC memory with initial state
npcMemory.SetValue(MemoryKeys.PlayerSentiment, Sentiment.Neutral);
npcMemory.SetValue(MemoryKeys.QuestFindLostAmulet_State, QuestState.NotStarted);
npcMemory.SetValue("TimesPlayerAnnoyed", 0);

// --- Define Rules ---

// Rule 1: High priority annoyance rule
rules.Add(new Rule(
    name: "NPC Annoyed",
    criterias: new List<ICriteria> {
        new Criteria<int>("TimesPlayerAnnoyed", 2, Operator.GreaterThan) // Annoyed if > 2
    },
    payload: () => { dialogueOutput = "NPC: I don't have time for this. Go away!"; }
) { Priority = 10 }); // High priority

// Rule 2: Quest is completed
rules.Add(new Rule(
    name: "Quest Completed Dialogue",
    criterias: new List<ICriteria> {
        new Criteria<QuestState>(MemoryKeys.QuestFindLostAmulet_State, QuestState.Completed, Operator.Equal)
    },
    payload: () => { dialogueOutput = "NPC: Ah, thank you again for finding my amulet!"; }
));

// Rule 3: Quest accepted, player is friendly
rules.Add(new Rule(
    name: "Friendly Quest Reminder",
    criterias: new List<ICriteria> {
        new Criteria<QuestState>(MemoryKeys.QuestFindLostAmulet_State, QuestState.Accepted, Operator.Equal),
        new Criteria<Sentiment>(MemoryKeys.PlayerSentiment, Sentiment.Friendly, Operator.GreaterThanOrEqual) // Friendly or Allied
    },
    payload: () => { dialogueOutput = "NPC: How goes the search for my amulet, friend?"; }
));

// Rule 4: Quest accepted, player is neutral or worse
rules.Add(new Rule(
    name: "Neutral Quest Reminder",
    criterias: new List<ICriteria> {
        new Criteria<QuestState>(MemoryKeys.QuestFindLostAmulet_State, QuestState.Accepted, Operator.Equal),
        new Criteria<Sentiment>(MemoryKeys.PlayerSentiment, Sentiment.Neutral, Operator.Equal) // Neutral only
    },
    payload: () => { dialogueOutput = "NPC: Still looking for that amulet, I presume?"; }
));

// Rule 5: Default friendly greeting
rules.Add(new Rule(
    name: "Default Friendly",
    criterias: new List<ICriteria> {
        new Criteria<Sentiment>(MemoryKeys.PlayerSentiment, Sentiment.Friendly, Operator.Equal)
    },
    payload: () => { dialogueOutput = "NPC: Good day to you!"; }
));

// --- Optimize and Match ---
rules.OptimizeRules(); // Sort rules by criteria count (recommended by SFPM)

Console.WriteLine("--- Initial Dialogue ---");
rules.Match(npcMemory); // Match directly using Memory as IFactSource
Console.WriteLine(dialogueOutput); // Output: NPC: Hello there. (No rule matched initially)

// --- Simulate Game Events ---

// Player accepts the quest
npcMemory.SetValue(MemoryKeys.QuestFindLostAmulet_State, QuestState.Accepted);
Console.WriteLine("\n--- Player accepted quest (Neutral) ---");
rules.Match(npcMemory);
Console.WriteLine(dialogueOutput); // Output: NPC: Still looking for that amulet, I presume?

// Player becomes friendly
npcMemory.SetValue(MemoryKeys.PlayerSentiment, Sentiment.Friendly);
Console.WriteLine("\n--- Player became friendly ---");
rules.Match(npcMemory);
Console.WriteLine(dialogueOutput); // Output: NPC: How goes the search for my amulet, friend?

// Player annoys the NPC 3 times
npcMemory.SetValue("TimesPlayerAnnoyed", 3);
Console.WriteLine("\n--- Player annoyed NPC ---");
rules.Match(npcMemory);
Console.WriteLine(dialogueOutput); // Output: NPC: I don't have time for this. Go away! (Priority rule overrides)

// Player completes quest
npcMemory.SetValue(MemoryKeys.QuestFindLostAmulet_State, QuestState.Completed);
npcMemory.SetValue("TimesPlayerAnnoyed", 0); // Reset annoyance
npcMemory.SetValue(MemoryKeys.PlayerSentiment, Sentiment.Allied); // Player is now liked
Console.WriteLine("\n--- Player completed quest ---");
rules.Match(npcMemory);
Console.WriteLine(dialogueOutput); // Output: NPC: Ah, thank you again for finding my amulet!
```

## Performance Considerations

- **Avoid Per-Frame Access:** The `Memory` class is designed for storing data that doesn't need to be accessed or iterated over every single frame in a high-performance loop. While reads and writes are reasonably fast and thread-safe, frequent access in tight loops might incur more overhead than direct field access or Component Data in an ECS. This does not mean you cant use the system in combination with the stella fuzzy pattern matcher to dispatch an event every frame. Keep simply in mind that using it is slower than pre defining components for entities. The high dynamic nature of memory makes it just really simple to add some metadata at runtime to entities.
- **Use Case:** It excels when used for event-driven checks, storing dialogue states, quest progress, infrequent observations, or other less critical metadata.

## API

- `Memory()`: Constructor.
- `SetValue<TKey, TValue>(TKey key, TValue value)`: Adds or updates a fact. Thread-safe. Returns `Memory` for chaining.
- `TryGetValue<TKey, TValue>(TKey key, out TValue value)`: Tries to get a fact's value. Thread-safe.
- `GetValue<TKey, TValue>(TKey key)`: Gets a fact's value; throws `KeyNotFoundException` if missing. Thread-safe.
- `ContainsKey<TKey, TValue>(TKey key)`: Checks if a fact exists. Thread-safe.
- `RemoveValue<TKey, TValue>(TKey key)`: Removes a fact. Thread-safe.
- `Clear<TKey, TValue>()`: Clears all facts of a specific type combination. Thread-safe.
- `ClearAll()`: Removes all facts of all types. Thread-safe.
- `For<TKey, TValue>()`: Returns a `TypedMemoryAccessor` struct for indexer access (`memory.For<string,int>()["key"]`). Thread-safe via delegation.
- `TryGetFact<TValue>(string factName, out TValue value)`: Implements `IFactSource` for SFPM. Delegates to `TryGetValue<string, TValue>`.

```

```
