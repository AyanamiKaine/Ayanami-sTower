using Flecs.NET.Core;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using System;


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

var vault = entities.GetEntityCreateIfNotExist("KnowledgeVaultPage")
    .Add<Page>()
    .Set(new Grid())
    .SetRow(2)
    .SetColumnSpan(3);

var vaultContent = entities.GetEntityCreateIfNotExist("VaultContent")
    .ChildOf(vault)
    .Set(new TextBlock())
    .SetText("VaultContent")
    .SetRow(1)
    .SetColumn(0)
    .SetColumnSpan(3);