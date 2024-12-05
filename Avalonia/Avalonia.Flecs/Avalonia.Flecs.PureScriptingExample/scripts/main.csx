using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;


var window = world.Lookup("MainWindow");


var grid = entities["Grid"]
    .Set(new Grid())
    .ChildOf(window)
    .SetColumnDefinitions("*,*");

entities["Button2"]
    .Set(new Button())
    .ChildOf(grid)
    .SetColumn(0)
    .SetContent("CLICK ME AGAIN");


var button = entities["Button"]
    .Set(new Button())
    .SetContent("CLICK ME AGAIN")
    .ChildOf(grid)
    .SetColumn(1)
    .OnClick((sender, args) =>
    {
        Console.WriteLine("HEY");
    })
    .SetMargin(10);