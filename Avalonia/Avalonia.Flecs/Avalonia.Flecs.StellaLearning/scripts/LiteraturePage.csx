using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Flecs.NET.Core;
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

var literaturePage = entities.GetEntityCreateIfNotExist("LiteraturePage")
    .Add<Page>()
    .Set(new Grid())
    .SetColumnDefinitions(new ColumnDefinitions("*, Auto, Auto"))
    .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));