using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;


var window = world.Lookup("MainWindow");


/*
We could probably add a check like if entity exists, do this, and if it doesn't, do that.
so we limit the actions that SHOULD NOT BE DONE TWICE on an existing entity. 
*/

var button = entities["Button"]
    .Set(new Button())
    .SetContent("CLICK ME")
    .ChildOf(window)
    .OnClick((sender, args) =>
    {
        Console.WriteLine("YOU CLICKED ME");
    });