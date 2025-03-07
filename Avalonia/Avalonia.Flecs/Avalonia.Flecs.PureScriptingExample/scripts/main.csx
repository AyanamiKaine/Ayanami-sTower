using Flecs.NET.Core;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.Controls.ECS;
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
/*
You can even define new "methods" for entities IN SCRIPTS WHOA!
More and more it morphs into a programming style of change code
see changes, get feedback, make new changes. Something I only felt
using Lisp (Clojure or Common Lisp) in Emacs. 
*/
static Entity Test(this Entity entity)
{
    Console.WriteLine(entity);
    return entity;
}

var window = world.Lookup("MainWindow");


/*
We could probably add a check like if entity exists, do this, and if it doesn't, do that.
so we limit the actions that SHOULD NOT BE DONE TWICE on an existing entity. 
*/


var grid = entities["Grid"]
    .Set(new Grid())
    .ChildOf(window)
    .SetColumnDefinitions("*,*")
    .Test();

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

var button3 = entities["Button3"]
    .Set(new Button())
    .SetContent("CLICK ME AGAIN")
    .ChildOf(grid)
    .SetColumn(1)
    .OnClick((sender, args) =>
    {
        Console.WriteLine("HEY");
    })
    .SetMargin(10);