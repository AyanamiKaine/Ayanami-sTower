using Flecs.NET.Core;

namespace FlecsExploration;

file record struct Name(string Value);
file record struct Age(int Amount);

file record struct Credits(double Amount);
file struct Character;

/// <summary>
/// Sometimes we want to create templates for entities,
/// with a specific set of predefined components.
/// We do this by using an entity prefab.
/// </summary>
public class EntityTemplates
{

    [Fact]
    public void CreatingATemplate()
    {
        World world = World.Create();

        // Here we define a character template
        var characterPrefab = world.Prefab("Character")
            .Add<Character>(new())
            .Set<Credits>(new(0))
            .Set<Age>(new(0))
            .Set<Name>(new("DEFAULT_NAME"));

        var melina = world.Entity("Melina").IsA(characterPrefab);

        Assert.True(
            melina.Has<Age>() &&
            melina.Has<Name>() &&
            melina.Has<Character>());

    }
}