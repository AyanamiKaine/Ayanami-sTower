using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;


var window = world.Lookup("MainWindow");

var button = entities["Button"]
    .Set(new Button())
    .SetContent("CLICK ME")
    .ChildOf(window)
    .OnClick((sender, args) =>
    {
        Console.WriteLine("YOU CLICKED ME");
    });