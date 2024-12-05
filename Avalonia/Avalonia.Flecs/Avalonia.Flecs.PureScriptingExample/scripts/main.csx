using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Controls;
using System;


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