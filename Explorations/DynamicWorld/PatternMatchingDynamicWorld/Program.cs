using AyanamisTower.NihilEx;
using AyanamisTower.SFPM;

namespace DynamicWorld;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


public class Location : IComparable<Location>
{
    public string Name { get; }

    public static readonly Location DungeonCell = new Location("Dungeon Cell");
    public static readonly Location Hallway = new Location("Hallway");
    public static readonly Location GuardRoom = new Location("Guard Room");
    public static readonly Location Outside = new Location("Outside"); // WINNING LOCATION!

    private Location(string name) { Name = name; }
    public int CompareTo(Location? other)
    {
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
    public override string ToString() { return Name; }
}

public class GameSimulation
{
    private readonly Memory _worldState;
    private readonly List<Rule> _gameRules;
    private readonly Query _query;
    private bool _gameOver = false;

    public GameSimulation()
    {
        _worldState = new Memory();
        _gameRules = new List<Rule>();
        _query = new Query(_worldState);
    }

    private void SetupWorld()
    {
        Console.WriteLine("Setting up world...");
        _worldState.SetValue("PlayerLocation", Location.DungeonCell)
                   .SetValue("HasRustyKey", false)
                   .SetValue("CellDoorUnlocked", false)
                   .SetValue("GuardIsAwake", true)
                   .SetValue("HasRock", false) // New item
                   .SetValue("HasMainKey", false) // Key to escape
                   .SetValue("GameCommand", "");
    }

    private void SetupRules()
    {
        Console.WriteLine("Defining game logic as rules...");

        // --- META COMMANDS ---
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "quit", Operator.Equal) },
            () => { Console.WriteLine("\nThanks for playing!"); _gameOver = true; }));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "help", Operator.Equal) },
            () => Console.WriteLine("\nAvailable commands: look, take [item], unlock door, throw rock, go [direction], quit.")));

        // --- LOOK RULES ---
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "look", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal) },
            () => Console.WriteLine("\nYou are in a damp, cold dungeon cell. A heavy wooden door is to the north. In the corner, you see a glint of metal on a small pile of straw. A loose rock sits by the wall.")));
        _gameRules.Add(new Rule([new Criteria<string>("GameCommand", "look", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal)],
            () => Console.WriteLine("\nYou are in a narrow stone hallway. Your cell is to the south. The hallway continues to the east, where you see a wooden door. To the west is a heavy iron door leading outside.")));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "look", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.GuardRoom, Operator.Equal), new Criteria<bool>("GuardIsAwake", true, Operator.Equal) },
            () => Console.WriteLine("\nYou peek into the guard room. A burly guard is sitting at a table, watching you carefully. A large iron key hangs on a hook on the far wall.")));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "look", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.GuardRoom, Operator.Equal), new Criteria<bool>("GuardIsAwake", false, Operator.Equal) },
            () => Console.WriteLine("\nYou are in the guard room. The guard is slumped over the table, fast asleep. A large iron key hangs on a hook on the wall.")));

        // --- ITEM INTERACTION RULES ---
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "take key", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal) },
            () => { Console.WriteLine("\nYou search the straw and find a small, rusty key."); _worldState.SetValue("HasRustyKey", true); })
        { Priority = 10 });
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "take key", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.GuardRoom, Operator.Equal), new Criteria<bool>("GuardIsAwake", false, Operator.Equal) },
            () => { Console.WriteLine("\nYou sneak over and quietly lift the main key from the hook."); _worldState.SetValue("HasMainKey", true); })
        { Priority = 10 });
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "take key", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.GuardRoom, Operator.Equal), new Criteria<bool>("GuardIsAwake", true, Operator.Equal) },
            () => { Console.WriteLine("\nYou reach for the key, but the guard shouts and raises his axe. You have failed."); _gameOver = true; })
        { Priority = 10 }); // GAME OVER
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "take rock", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal) },
            () => { Console.WriteLine("\nYou pick up a fist-sized rock."); _worldState.SetValue("HasRock", true); }));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "throw rock", Operator.Equal), new Criteria<bool>("HasRock", true, Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal) },
            () => { Console.WriteLine("\nYou throw the rock down the hallway. It makes a loud clatter. You hear the guard in the east room stir and get up to investigate."); _worldState.SetValue("GuardIsAwake", false); _worldState.SetValue("HasRock", false); })
        { Priority = 5 });

        // --- DOOR AND MOVEMENT RULES ---
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "unlock door", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal), new Criteria<bool>("HasRustyKey", true, Operator.Equal) },
            () => { Console.WriteLine("\nThe rusty key fits the lock perfectly. With a loud *clunk*, the cell door unlocks."); _worldState.SetValue("CellDoorUnlocked", true); })
        { Priority = 10 });
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go north", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal), new Criteria<bool>("CellDoorUnlocked", true, Operator.Equal) },
            () => { Console.WriteLine("\nYou open the door and step into a hallway."); _worldState.SetValue("PlayerLocation", Location.Hallway); })
        { Priority = 10 });
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go north", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.DungeonCell, Operator.Equal) },
            () => Console.WriteLine("\nThe door is locked.")));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go east", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal) },
            () => _worldState.SetValue("PlayerLocation", Location.GuardRoom)));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go south", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal) },
            () => _worldState.SetValue("PlayerLocation", Location.DungeonCell)));
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go west", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal), new Criteria<bool>("HasMainKey", true, Operator.Equal) },
            () => { Console.WriteLine("\nThe main key slides into the heavy iron door. With a great effort, you turn it and the door swings open to reveal the moonlit forest. You are free!"); _worldState.SetValue("PlayerLocation", Location.Outside); _gameOver = true; })
        { Priority = 10 }); // WIN!
        _gameRules.Add(new Rule(new List<ICriteria> { new Criteria<string>("GameCommand", "go west", Operator.Equal), new Criteria<Location>("PlayerLocation", Location.Hallway, Operator.Equal) },
            () => Console.WriteLine("\nA heavy iron door blocks your way. It looks like it needs a large, important key.")));

    }

    public void Run()
    {
        SetupWorld();
        SetupRules();

        Console.WriteLine("\n--- Welcome to Dungeon Escape! ---");
        Console.WriteLine("Type 'help' for a list of commands.");

        _worldState.SetValue("GameCommand", "look");
        _query.Match(_gameRules);

        while (!_gameOver)
        {
            Console.Write("\n> ");
            string? input = Console.ReadLine()?.ToLower().Trim();

            if (!string.IsNullOrEmpty(input))
            {
                _worldState.SetValue("GameCommand", input);
                _query.Match(_gameRules);
            }
        }
    }

    public static void Main(string[] args)
    {
        var game = new GameSimulation();
        game.Run();
    }
}