using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;

World ecsWorld = _world;
Entity window = ecsWorld.Lookup("MainWindow");

window.SetWindowTitle("Stella Learning")
      .SetHeight(500);

