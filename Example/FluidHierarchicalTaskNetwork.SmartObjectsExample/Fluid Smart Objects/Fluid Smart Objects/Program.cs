using Fluid;

Console.WriteLine("At location 0");

var world = new World();
var player = new Player(world);

player.Think();
player.Think();

Console.WriteLine("The End!");
