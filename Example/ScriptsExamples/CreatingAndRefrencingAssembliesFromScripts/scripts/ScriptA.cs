using System;
using Flecs;
using Flecs.NET.Core;
using ModuleA; // Assuming utils.cs defines things in 'MyUtils' namespace

/// <summary>
/// Script A
/// </summary>
public class ScriptA
{
    /// <summary>
    /// Runs the script
    /// </summary>
    public void Run()
    {
        var e = GlobalData.world.Entity("Global ENTITY CREATED IN SCRIPT A");
        e.Set<Utility>(new());
        Console.WriteLine($"Entity was created with name '{e.Name()}'");

        Console.WriteLine("Main script running.");
        var helper = new Utility();
        helper.DoSomethingUseful();
    }
}
