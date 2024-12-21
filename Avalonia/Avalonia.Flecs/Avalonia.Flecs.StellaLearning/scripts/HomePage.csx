// By default script directives (#r) are being removed
// from the script before compilation. We are just doing this here 
// so the C# Devkit and Omnisharp (For what every reason the libraries.rsp 
// does not get used any more, new bug?) can provide us with autocompletion and analysis of the code
#r "../bin/Debug/net9.0/Avalonia.Base.dll"
#r "../bin/Debug/net9.0/Avalonia.FreeDesktop.dll"
#r "../bin/Debug/net9.0/Avalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Desktop.dll"
#r "../bin/Debug/net9.0/Avalonia.X11.dll"
#r "../bin/Debug/net9.0/FluentAvalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Markup.Xaml.dll"
#r "../bin/Debug/net9.0/Flecs.NET.dll"
#r "../bin/Debug/net9.0/Flecs.NET.Bindings.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.xml"
#r "../bin/Debug/net9.0/Avalonia.Flecs.FluentUI.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.StellaLearning.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.xml"

using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.Controls.ECS.Module;

/// <summary>
/// We can refrence the ecs world via _world its globally available in all scripts
/// we assing world = _world so the language server knows the world exists and
/// can provide us with autocompletion and correct showcase of possible compile errors
/// </summary>
public World world = _world;
/// <summary>
/// We can refrence the named entities via _entities its globally available in all scripts
/// we assing entities = _entities so the language server knows the named entities exists and
/// can provide us with autocompletion and correct showcase of possible compile errors
/// </summary>
public NamedEntities entities = _entities;
var homePage = entities.GetEntityCreateIfNotExist("HomePage")
            .Add<Page>()
            .Set(new TextBlock())
            .SetText("Home");