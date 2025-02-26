using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// Pipeline is a collection of systems that are executed in specifc sequence.
/// Built-in Piples lines are:
/// Ecs.OnStart
/// Ecs.OnLoad
/// Ecs.PostLoad
/// Ecs.PreUpdate
/// Ecs.OnUpdate
/// Ecs.OnValidate
/// Ecs.PostUpdate
/// Ecs.PreStore
/// Ecs.OnStore
/// 
/// We can define our own pipelines to ensure that some systems always run before others.
/// </summary>
public class Pipelines
{

    [Fact]
    public void CreatingPipeline()
    {
        World world = World.Create();
    }
}