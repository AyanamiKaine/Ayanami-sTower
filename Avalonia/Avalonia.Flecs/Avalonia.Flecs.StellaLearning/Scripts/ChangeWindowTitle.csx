using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;


Entity window = world.Lookup("MainWindow");
window.SetWindowTitle("Stella Learning");
window.SetHeight(500);
Console.WriteLine(window.Name());


var entity = world.Entity("NewWindow")
    .Set(new Window())
    .SetWindowTitle("New Window")
    .ShowWindow();

Console.WriteLine(entity.Type().Str() + "\n");
