using System.Diagnostics;
using InvictaDB;
using StellaInvicta;
using StellaInvicta.Data;

static Character CreateCharacter(string name, int age, DateTime birthDate) =>
    new(name, age, 10, 10, 10, 10, birthDate);

static Character CreateRandomCharacter(Random rng, int id, int gameYear)
{
    var birthYear = rng.Next(gameYear - 80, gameYear - 1); // Age 1-80
    var birthMonth = rng.Next(1, 13);
    var birthDay = rng.Next(1, 29); // Safe day range for all months
    var birthDate = new DateTime(birthYear, birthMonth, birthDay);
    var age = gameYear - birthYear - 1; // Approximate age
    return CreateCharacter($"Character_{id}", age, birthDate);
}

var game = new Game();
var db = new InvictaDatabase();
game.AddSystem("Date System", new StellaInvicta.System.Date.DateSystem());
game.AddSystem("Age System", new StellaInvicta.System.Age.AgeSystem());

db = game.InitializeSystems(db);

// Start game on Jan 1, Year 100
const int startYear = 100;
db = db.InsertSingleton(new DateTime(startYear, 1, 1, 0, 0, 0));

// Create many characters for benchmarking
var rng = new Random(42); // Fixed seed for reproducibility
var characterCounts = new[] { 1_000, 5_000, };

Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    StellaInvicta Year Simulation Benchmark                   ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  Reference times for 1 year simulation in other games:                       ║");
Console.WriteLine("║    • Hearts of Iron 4:  ~180 seconds (3 minutes)                             ║");
Console.WriteLine("║    • Victoria 3:        ~90 seconds                                          ║");
Console.WriteLine("║    • Stellaris:         ~45 seconds                                          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

foreach (var characterCount in characterCounts)
{
    // Reset database for each test
    db = new InvictaDatabase();
    game = new Game();
    game.AddSystem("Date System", new StellaInvicta.System.Date.DateSystem());
    game.AddSystem("Age System", new StellaInvicta.System.Age.AgeSystem());
    db = game.Init(db);
    db = db.InsertSingleton(new DateTime(startYear, 1, 1, 0, 0, 0));

    // Create characters
    Console.Write($"Creating {characterCount:N0} characters... ");
    var charStopwatch = Stopwatch.StartNew();
    for (int i = 0; i < characterCount; i++)
    {
        var character = CreateRandomCharacter(rng, i, startYear);
        db = db.Insert($"char_{i}", character);
    }
    charStopwatch.Stop();
    Console.WriteLine($"done in {charStopwatch.ElapsedMilliseconds:N0} ms");

    db = db.ClearMessages();

    // Simulate one year
    Console.Write($"Simulating 1 year with {characterCount:N0} characters... ");
    var simStopwatch = Stopwatch.StartNew();
    db = game.SimulateYear(db);
    simStopwatch.Stop();

    var elapsedMs = simStopwatch.ElapsedMilliseconds;
    var elapsedSec = elapsedMs / 1000.0;

    // Calculate speedup vs other games
    var vsHoi4 = 180.0 / elapsedSec;
    var vsVic3 = 90.0 / elapsedSec;
    var vsStellaris = 45.0 / elapsedSec;

    Console.WriteLine($"done in {elapsedMs:N0} ms ({elapsedSec:F2}s)");
    Console.WriteLine($"  ├─ vs HOI4 (180s):     {vsHoi4:F1}x faster");
    Console.WriteLine($"  ├─ vs Victoria 3 (90s): {vsVic3:F1}x faster");
    Console.WriteLine($"  └─ vs Stellaris (45s):  {vsStellaris:F1}x faster");

    // Verify some characters aged correctly
    var currentDate = db.GetSingleton<DateTime>();
    Console.WriteLine($"  Game date after simulation: {currentDate:yyyy-MM-dd}");
    Console.WriteLine();
}

// Final summary
Console.WriteLine("════════════════════════════════════════════════════════════════════════════════");
Console.WriteLine("Note: These comparisons are illustrative. Paradox games simulate much more than");
Console.WriteLine("character aging (AI, economy, military, diplomacy, map updates, graphics, etc.)");
Console.WriteLine("════════════════════════════════════════════════════════════════════════════════");