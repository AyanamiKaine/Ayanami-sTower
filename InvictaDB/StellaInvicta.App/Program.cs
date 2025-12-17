using System.Diagnostics;
using InvictaDB;
using StellaInvicta;

var game = new Game();
var db = new InvictaDatabase();
game.AddSystem("Date System", new StellaInvicta.System.Date.DateSystem());
db = game.InitializeSystems(db);

var stopwatch = Stopwatch.StartNew();
db = game.SimulateYear(db);
stopwatch.Stop();

Console.WriteLine($"Simulated year in {stopwatch.ElapsedMilliseconds} ms");