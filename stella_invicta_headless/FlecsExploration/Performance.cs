
using Flecs.NET.Core;

namespace FlecsExploration;

public class Performance
{
    /// <summary>
    /// By default, Flecs is single-threaded. We can set the number of worker threads to use when we define
    /// a system to be multithreaded. Again by default systems are single-threaded.
    /// </summary>
    [Fact]
    public void SettingWorkerThreads()
    {
        World world = World.Create();
        world.SetThreads(32);
        // We must manually define the number of threads Flecs should use.
        // A good choice would be choice is the number of threads equal to the number of cores on the machine.
        // world.SetThreads(Environment.ProcessorCount);
    }

    [Fact]
    public void MultithreadedSystem()
    {
        World world = World.Create();
        world.SetThreads(Environment.ProcessorCount);

        world.System("Multithreaded System")
        .MultiThreaded(true) // To create a multithreaded system we call the MultiThreaded method on a system.
        .Each(() =>
        {

        });
    }
}