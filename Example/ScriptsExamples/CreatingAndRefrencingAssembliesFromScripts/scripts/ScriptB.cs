using System;
using Flecs.NET.Core;
using ModuleA;

/// <summary>
/// Script B
/// </summary>
public class ScriptB
{
    /// <summary>
    /// Does something ...
    /// </summary>
    public void PerformAction()
    {
        var e = GlobalData.world.Entity("ScriptB Entity");

        Console.WriteLine($"{e.Name()}");
        e.Set<Utility>(new());

        Console.WriteLine("Secondary script starting its action.");
        // Create an instance of the Utility class from utils.cs
        var util = new Utility();
        // Call a method from the Utility class
        util.DoSomethingUseful();

        Console.WriteLine("Secondary script finished its action.");

        GlobalData.world.Each<Utility>(
            (Entity e, ref Utility _) =>
            {
                Console.WriteLine(e.Name());
            }
        );
    }

    // You could potentially have a Main method here too if needed,
    // although often in hosting scenarios, specific methods are called by the host.
    // public static void Main(string[] args)
    // {
    //     var instance = new SecondaryScript();
    //     instance.PerformAction();
    // }
}
